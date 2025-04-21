using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Nova
{
    public class CheckpointManager : MonoBehaviour
    {
        [SerializeField] private string saveFolder = "";

        // When constructed in editor, e.g. by SaveViewer, it avoids modifying the global save file
        private bool frozen;

        private string savePathBase;
        private string globalSavePath;
        private string backupPath;

        private GlobalSave globalSave;
        private bool globalSaveDirty;

        // nodeName => dialogueIndex => data
        private readonly Dictionary<string, List<ReachedDialogueData>> reachedDialogues =
            new Dictionary<string, List<ReachedDialogueData>>();

        private readonly SerializableHashSet<string> reachedEnds = new SerializableHashSet<string>();

        private readonly Dictionary<int, Bookmark> cachedBookmarks = new Dictionary<int, Bookmark>();
        public readonly Dictionary<int, BookmarkMetadata> bookmarksMetadata = new Dictionary<int, BookmarkMetadata>();

        private CheckpointSerializer serializer;

        private bool inited;

        #region Initialization

        // Needs to be called in Awake
        public void Init()
        {
            if (inited)
            {
                return;
            }

            frozen = !Application.isPlaying;

            savePathBase = Path.Combine(Application.persistentDataPath, "Save", saveFolder);
            globalSavePath = Path.Combine(savePathBase, "global.nsav");
            backupPath = Path.Combine(savePathBase, "global.nsav.bak");
            Directory.CreateDirectory(savePathBase);

            serializer = new CheckpointSerializer(globalSavePath, frozen);
            if (!File.Exists(globalSavePath))
            {
                ResetGlobalSave();
            }
            else
            {
                TryInitGlobalSaveReached();
                if (globalSave == null)
                {
                    Debug.LogError("Nova: Failed to init global save. Trying to restore...");
                    RestoreGlobalSave();
                }
            }

            bookmarksMetadata.Clear();
            foreach (string fileName in Directory.GetFiles(savePathBase, "sav*.nsav"))
            {
                var result = Regex.Match(fileName, @"sav([0-9]+)\.nsav");
                if (result.Groups.Count > 1 && int.TryParse(result.Groups[1].Value, out int id))
                {
                    bookmarksMetadata.Add(id, new BookmarkMetadata
                    {
                        saveID = id,
                        modifiedTime = File.GetLastWriteTime(fileName)
                    });
                }
            }

            // Debug.Log("Nova: CheckpointManager initialized.");
            inited = true;
        }

        private void Awake()
        {
            Init();
        }

        private void OnDestroy()
        {
            UpdateGlobalSave();

            foreach (var bookmark in cachedBookmarks.Values)
            {
                bookmark.DestroyTexture();
            }

            serializer.Dispose();
        }

        #endregion

        #region Common methods

        public long NextRecord(long offset)
        {
            return serializer.NextRecord(offset);
        }

        #endregion

        #region Reached data

        private void InitReached()
        {
            reachedDialogues.Clear();
            reachedEnds.Clear();
            for (var cur = globalSave.beginReached; cur < globalSave.endReached; cur = NextRecord(cur))
            {
                var record = serializer.DeserializeRecord<IReachedData>(cur);
                if (record is ReachedEndData end)
                {
                    reachedEnds.Add(end.endName);
                }
                else if (record is ReachedDialogueData dialogue)
                {
                    SetReachedDialogueData(dialogue);
                }
                else if (record is NodeUpgradeMaker maker)
                {
                    reachedDialogues.Remove(maker.nodeName);
                }
                else
                {
                    Debug.LogWarning($"Nova: Unknown record type {record.GetType()} at offset {cur}");
                }
            }

            // check each reached data is a prefix
            foreach (var reachedList in reachedDialogues)
            {
                if (reachedList.Value.Contains(null))
                {
                    throw CheckpointCorruptedException.BadReachedData(reachedList.Key);
                }
            }
        }

        private void UpdateEndReached()
        {
            globalSave.endReached = NextRecord(globalSave.endReached);
            globalSaveDirty = true;
            // Debug.Log($"next reached {globalSave.endReached}");
        }

        private void SetReachedDialogueData(ReachedDialogueData data)
        {
            var list = reachedDialogues.Ensure(data.nodeName);
            list.Ensure(data.dialogueIndex + 1);
            list[data.dialogueIndex] = data;
        }

        private void AppendReachedRecord(IReachedData data)
        {
            serializer.SerializeRecord(globalSave.endReached, data);
            UpdateEndReached();
        }

        public void SetReached(ReachedDialogueData data)
        {
            if (IsReachedAnyHistory(data.nodeName, data.dialogueIndex))
            {
                return;
            }

            SetReachedDialogueData(data);
            AppendReachedRecord(data);
        }

        public void SetReachedEnd(string endName)
        {
            if (reachedEnds.Contains(endName))
            {
                return;
            }

            reachedEnds.Add(endName);
            AppendReachedRecord(new ReachedEndData(endName));
        }

        public bool IsReachedAnyHistory(string nodeName, int dialogueIndex)
        {
            return reachedDialogues.ContainsKey(nodeName) &&
                   dialogueIndex < reachedDialogues[nodeName].Count &&
                   reachedDialogues[nodeName][dialogueIndex] != null;
        }

        public ReachedDialogueData GetReachedDialogueData(string nodeName, int dialogueIndex)
        {
            return reachedDialogues[nodeName][dialogueIndex];
        }

        public bool IsReachedEnd(string endName)
        {
            return reachedEnds.Contains(endName);
        }

        public void InvalidateReachedData(string nodeName)
        {
            reachedDialogues.Remove(nodeName);
            AppendReachedRecord(new NodeUpgradeMaker(nodeName));
        }

        #endregion

        #region Checkpoint

        public long beginCheckpoint
        {
            get => globalSave.beginCheckpoint < globalSave.endCheckpoint ? globalSave.beginCheckpoint : 0;
            set
            {
                globalSave.beginCheckpoint = value;
                globalSaveDirty = true;
            }
        }

        public long endCheckpoint => globalSave.endCheckpoint;

        private void UpdateEndCheckpoint()
        {
            globalSave.endCheckpoint = NextRecord(globalSave.endCheckpoint);
            globalSaveDirty = true;
            // Debug.Log($"next checkpoint {globalSave.endCheckpoint}");
        }

        public long NextCheckpoint(long offset)
        {
            return NextRecord(NextRecord(offset));
        }

        // Get or create the next node record
        public NodeRecord GetNextNodeRecord(NodeRecord prevRecord, string name, Variables variables, int beginDialogue)
        {
            var variablesHash = variables.hash;
            NodeRecord childRecord = null;
            var offset = prevRecord?.child ?? globalSave.beginCheckpoint;
            while (offset != 0 && offset < globalSave.endCheckpoint)
            {
                childRecord = GetNodeRecord(offset);
                if (childRecord.name == name && childRecord.variablesHash == variablesHash)
                {
                    return childRecord;
                }

                offset = childRecord.sibling;
            }

            // Now childRecord is prevRecord's child's last sibling or null
            offset = globalSave.endCheckpoint;
            var newRecord = new NodeRecord(offset, name, beginDialogue, variablesHash);
            if (childRecord != null)
            {
                childRecord.sibling = offset;
                UpdateNodeRecord(childRecord);
            }
            else if (prevRecord != null)
            {
                prevRecord.child = offset;
                UpdateNodeRecord(prevRecord);
            }

            if (prevRecord != null)
            {
                newRecord.parent = prevRecord.offset;
            }

            UpdateNodeRecord(newRecord);
            UpdateEndCheckpoint();
            return newRecord;
        }

        public NodeRecord GetNodeRecord(long offset)
        {
            return new NodeRecord(offset, serializer.GetRecord(offset));
        }

        public void UpdateNodeRecord(NodeRecord record)
        {
            serializer.AppendRecord(record.offset, record.ToByteSegment());
        }

        public bool CanAppendCheckpoint(long offset)
        {
            return NextRecord(offset) >= globalSave.endCheckpoint ||
                   NextCheckpoint(offset) >= globalSave.endCheckpoint;
        }

        public void AppendDialogue(NodeRecord nodeRecord, int dialogueIndex, bool shouldSaveCheckpoint)
        {
            nodeRecord.endDialogue = dialogueIndex + 1;
            if (shouldSaveCheckpoint)
            {
                nodeRecord.lastCheckpointDialogue = dialogueIndex;
            }

            UpdateNodeRecord(nodeRecord);
        }

        public long AppendCheckpoint(int dialogueIndex, GameStateCheckpoint checkpoint)
        {
            var offset = globalSave.endCheckpoint;

            var buf = new ByteSegment(4);
            buf.WriteInt(0, dialogueIndex);
            serializer.AppendRecord(globalSave.endCheckpoint, buf);
            UpdateEndCheckpoint();

            serializer.SerializeRecord(globalSave.endCheckpoint, checkpoint);
            UpdateEndCheckpoint();

            return offset;
        }

        public int GetCheckpointDialogueIndex(long offset)
        {
            return serializer.GetRecord(offset).ReadInt(0);
        }

        public GameStateCheckpoint GetCheckpoint(long offset)
        {
            return serializer.DeserializeRecord<GameStateCheckpoint>(NextRecord(offset));
        }

        #endregion

        #region Checkpoint upgrade

        public bool CheckScriptUpgrade(FlowChartGraph flowChartGraph, out Dictionary<string, Differ> changedNodes)
        {
            changedNodes = new Dictionary<string, Differ>();
            var updateHashes = false;
            if (globalSave.nodeHashes != null)
            {
                foreach (var node in flowChartGraph)
                {
                    if (globalSave.nodeHashes.ContainsKey(node.name) &&
                        globalSave.nodeHashes[node.name] != node.textHash &&
                        reachedDialogues.TryGetValue(node.name, out var dialogue))
                    {
                        updateHashes = true;
                        ScriptLoader.AddDeferredDialogueChunks(node);
                        var differ = new Differ(node, dialogue);
                        differ.GetDiffs();

                        Debug.Log($"Nova: Node {node.name} needs upgrade.");
                        changedNodes.Add(node.name, differ);
                    }
                }

                foreach (var node in globalSave.nodeHashes.Keys)
                {
                    if (!flowChartGraph.HasNode(node))
                    {
                        updateHashes = true;
                        Debug.Log($"Nova: Node {node} needs delete.");
                        changedNodes.Add(node, null);
                    }
                }
            }

            if (updateHashes || globalSave.nodeHashes == null)
            {
                globalSave.identifier = DateTime.Now.ToBinary();
                globalSave.nodeHashes = flowChartGraph.ToDictionary(node => node.name, node => node.textHash);
                globalSaveDirty = true;
            }

            return changedNodes.Any();
        }

        public long UpgradeNodeRecord(NodeRecord nodeRecord, int beginDialogue)
        {
            var beginCheckpoint = GetCheckpoint(NextRecord(nodeRecord.offset));
            beginCheckpoint.dialogueIndex = beginDialogue;

            nodeRecord.beginDialogue = beginDialogue;
            nodeRecord.endDialogue = beginDialogue + 1;
            nodeRecord.lastCheckpointDialogue = beginDialogue;
            nodeRecord.offset = globalSave.endCheckpoint;
            UpdateNodeRecord(nodeRecord);
            UpdateEndCheckpoint();

            AppendCheckpoint(beginDialogue, beginCheckpoint);
            return nodeRecord.offset;
        }

        private void ResetChildParent(NodeRecord nodeRecord)
        {
            var offset = nodeRecord.child;
            while (offset != 0)
            {
                var child = GetNodeRecord(offset);
                child.parent = nodeRecord.offset;
                UpdateNodeRecord(child);
                offset = child.sibling;
            }
        }

        public void ResetChildParent(long offset)
        {
            ResetChildParent(GetNodeRecord(offset));
        }

        public long DeleteNodeRecord(NodeRecord nodeRecord)
        {
            nodeRecord.offset = 0;
            ResetChildParent(nodeRecord);
            return nodeRecord.sibling;
        }

        public bool IsNodeRecordTillEnd(NodeRecord nodeRecord)
        {
            if (nodeRecord.child != 0)
            {
                NodeRecord child = GetNodeRecord(nodeRecord.child);
                if (child.name != nodeRecord.name)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Global save

        private void InitGlobalSave()
        {
            globalSave = serializer.DeserializeRecord<GlobalSave>(CheckpointSerializer.GlobalSaveOffset);
        }

        private void TryInitGlobalSaveReached()
        {
            try
            {
                serializer.Open();
                InitGlobalSave();
                InitReached();
            }
            catch (Exception e)
            {
                Debug.LogError($"Nova: Failed to init global save: {e}");
                globalSave = null;
            }
        }

        public void UpdateGlobalSave()
        {
            if (frozen)
            {
                return;
            }

            if (globalSaveDirty)
            {
                serializer.SerializeRecord(CheckpointSerializer.GlobalSaveOffset, globalSave);
                globalSaveDirty = false;
            }

            serializer.Flush();
            // backup now
            File.Copy(globalSavePath, backupPath, true);
        }

        /// <summary>
        /// Reset the global save file to clear all progress.
        /// Note that all bookmarks will be invalid.
        /// </summary>
        private void ResetGlobalSave()
        {
            var saveDir = new DirectoryInfo(savePathBase);
            foreach (var file in saveDir.GetFiles())
            {
                file.Delete();
            }

            if (!frozen)
            {
                serializer.Open();
            }

            globalSave = new GlobalSave(serializer);
            globalSaveDirty = true;
            InitReached();
        }

        public void RestoreGlobalSave()
        {
            serializer.Dispose();
            if (File.Exists(backupPath))
            {
                File.Copy(backupPath, globalSavePath, true);
                TryInitGlobalSaveReached();
                if (globalSave == null)
                {
                    Debug.LogError("Nova: Failed to restore global save. Trying to reset...");
                    serializer.Dispose();
                    ResetGlobalSave();
                }
            }
            else
            {
                Debug.LogError("Nova: Global save backup not found. Trying to reset...");
                ResetGlobalSave();
            }
        }

        public GlobalSave GetGlobalSave()
        {
            return globalSave;
        }

        #endregion

        #region Bookmarks

        private string GetBookmarkFileName(int saveID)
        {
            return Path.Combine(savePathBase, $"sav{saveID:D3}.nsav");
        }

        private Bookmark ReplaceCache(int saveID, Bookmark bookmark)
        {
            if (cachedBookmarks.TryGetValue(saveID, out var old))
            {
                if (old == bookmark)
                {
                    return bookmark;
                }

                Destroy(old.screenshot);
            }

            if (bookmark == null)
            {
                cachedBookmarks.Remove(saveID);
            }
            else
            {
                cachedBookmarks[saveID] = bookmark;
            }

            return bookmark;
        }

        public void SaveBookmark(int saveID, Bookmark bookmark, bool isUpgrading = false)
        {
            if (!isUpgrading)
            {
                var screenshot = new Texture2D(bookmark.screenshot.width, bookmark.screenshot.height,
                    bookmark.screenshot.format, false);
                screenshot.SetPixels32(bookmark.screenshot.GetPixels32());
                screenshot.Apply();
                bookmark.screenshot = screenshot;
            }

            bookmark.globalSaveIdentifier = globalSave.identifier;

            serializer.WriteBookmark(GetBookmarkFileName(saveID), isUpgrading ? bookmark : ReplaceCache(saveID, bookmark));
            UpdateGlobalSave();

            var metadata = bookmarksMetadata.Ensure(saveID);
            metadata.saveID = saveID;
            if (!isUpgrading)
            {
                metadata.modifiedTime = DateTime.Now;
            }
        }

        public Bookmark LoadBookmark(int saveID, bool cache = true)
        {
            var bookmark = serializer.ReadBookmark(GetBookmarkFileName(saveID));
            if (cache && bookmark.globalSaveIdentifier != globalSave.identifier)
            {
                Debug.LogWarning($"Nova: Save file is incompatible with the global save file. saveID: {saveID}");
                bookmark = null;
            }

            return cache ? ReplaceCache(saveID, bookmark) : bookmark;
        }

        public void DeleteBookmark(int saveID)
        {
            File.Delete(GetBookmarkFileName(saveID));
            bookmarksMetadata.Remove(saveID);
            ReplaceCache(saveID, null);
        }

        /// <summary>
        /// Load the contents of all existing bookmark in the given range eagerly.
        /// </summary>
        /// <param name="beginSaveID">The beginning of the range, inclusive.</param>
        /// <param name="endSaveID">The end of the range, exclusive.</param>
        public void EagerLoadRange(int beginSaveID, int endSaveID)
        {
            for (; beginSaveID < endSaveID; beginSaveID++)
            {
                if (bookmarksMetadata.ContainsKey(beginSaveID))
                    LoadBookmark(beginSaveID);
            }
        }

        /// <summary>
        /// Load / Save a bookmark by ID. Will use cached result if exists.
        /// </summary>
        /// <param name="saveID">ID of the bookmark.</param>
        /// <returns>The cached or loaded bookmark</returns>
        public Bookmark this[int saveID]
        {
            get
            {
                if (!bookmarksMetadata.ContainsKey(saveID))
                    return null;
                if (!cachedBookmarks.ContainsKey(saveID))
                    LoadBookmark(saveID);
                return cachedBookmarks[saveID];
            }

            set => SaveBookmark(saveID, value);
        }

        /// <summary>
        /// Query the ID of the latest / earliest bookmark.
        /// </summary>
        /// <param name="begin">Beginning ID of the query range, inclusive.</param>
        /// <param name="end">Ending ID of the query range, exclusive.</param>
        /// <param name="type">Type of this query.</param>
        /// <returns>The ID to query. If no bookmark is found in range, the return value will be "begin".</returns>
        public int QuerySaveIDByTime(int begin, int end, SaveIDQueryType type)
        {
            var filtered = bookmarksMetadata.Values.Where(m => m.saveID >= begin && m.saveID < end).ToList();
            if (!filtered.Any())
                return begin;
            if (type == SaveIDQueryType.Earliest)
                return filtered.Aggregate((agg, val) => agg.modifiedTime < val.modifiedTime ? agg : val).saveID;
            else
                return filtered.Aggregate((agg, val) => agg.modifiedTime > val.modifiedTime ? agg : val).saveID;
        }

        public int QueryMaxSaveID(int begin)
        {
            if (!bookmarksMetadata.Any())
            {
                return begin;
            }

            return Math.Max(bookmarksMetadata.Keys.Max(), begin);
        }

        public int QueryMinUnusedSaveID(int begin, int end = int.MaxValue)
        {
            int saveID = begin;
            while (saveID < end && bookmarksMetadata.ContainsKey(saveID))
            {
                ++saveID;
            }

            return saveID;
        }

        #endregion

        #region Auxiliary data

        /// <summary>
        /// Get global data
        /// </summary>
        /// <see cref="GlobalSave"/>
        /// <param name="key">the key of the data</param>
        /// <param name="defaultValue">the default value if the key is not found</param>
        /// <typeparam name="T">the type of the data</typeparam>
        /// <returns>the stored data</returns>
        public T Get<T>(string key, T defaultValue = default)
        {
            if (globalSave.data.TryGetValue(key, out var value))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Set global data
        /// </summary>
        /// <see cref="GlobalSave"/>
        /// <param name="key">the key of the data</param>
        /// <param name="value">the data to store</param>
        public void Set(string key, object value)
        {
            globalSave.data[key] = value;
            globalSaveDirty = true;
        }

        public IReadOnlyDictionary<string, object> GetAllData()
        {
            return globalSave.data;
        }

        #endregion
    }
}
