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

        private GlobalSave globalSave;
        private bool globalSaveDirty;

        // nodeName => dialogueIndex => data
        private readonly Dictionary<string, List<ReachedDialogueData>> reachedDialogues =
            new Dictionary<string, List<ReachedDialogueData>>();

        private readonly SerializableHashSet<string> reachedEnds = new SerializableHashSet<string>();

        private readonly Dictionary<int, Bookmark> cachedSaveSlots = new Dictionary<int, Bookmark>();
        public readonly Dictionary<int, BookmarkMetadata> saveSlotsMetadata = new Dictionary<int, BookmarkMetadata>();

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
            Directory.CreateDirectory(savePathBase);

            serializer = new CheckpointSerializer(globalSavePath);
            if (!File.Exists(globalSavePath))
            {
                ResetGlobalSave();
            }
            else
            {
                serializer.Open();
                InitGlobalSave();
                InitReached();
            }

            foreach (string fileName in Directory.GetFiles(savePathBase, "sav*.nsav*"))
            {
                var result = Regex.Match(fileName, @"sav([0-9]+)\.nsav");
                if (result.Groups.Count > 1 && int.TryParse(result.Groups[1].Value, out int id))
                {
                    saveSlotsMetadata.Add(id, new BookmarkMetadata
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

            foreach (var bookmark in cachedSaveSlots.Values)
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
            for (var cur = globalSave.beginReached; cur < globalSave.endReached; cur = serializer.NextRecord(cur))
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

        private void NewReached()
        {
            globalSave.endReached = serializer.NextRecord(globalSave.endReached);
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
            NewReached();
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

        public void SetEndReached(string endName)
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

        public bool IsEndReached(string endName)
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

        public long beginNodeOffset
        {
            get => globalSave.beginCheckpoint < globalSave.endCheckpoint ? globalSave.beginCheckpoint : 0;
            set
            {
                globalSave.beginCheckpoint = value;
                globalSaveDirty = true;
            }
        }

        private void NewCheckpointRecord()
        {
            globalSave.endCheckpoint = serializer.NextRecord(globalSave.endCheckpoint);
            globalSaveDirty = true;
            // Debug.Log($"next checkpoint {globalSave.endCheckpoint}");
        }

        public long NextCheckpoint(long offset)
        {
            return NextRecord(NextRecord(offset));
        }

        public NodeRecord GetNextNode(NodeRecord prevRecord, string name, Variables variables, int beginDialogue)
        {
            var variablesHash = variables.hash;
            NodeRecord record = null;
            var offset = prevRecord?.child ?? globalSave.beginCheckpoint;
            while (offset != 0 && offset < globalSave.endCheckpoint)
            {
                record = serializer.GetNodeRecord(offset);
                if (record.name == name && record.variablesHash == variablesHash)
                {
                    return record;
                }

                offset = record.sibling;
            }

            offset = globalSave.endCheckpoint;
            var newRecord = new NodeRecord(offset, name, beginDialogue, variablesHash);
            if (record != null)
            {
                record.sibling = offset;
                serializer.UpdateNodeRecord(record);
            }
            else if (prevRecord != null)
            {
                prevRecord.child = offset;
                serializer.UpdateNodeRecord(prevRecord);
            }

            if (prevRecord != null)
            {
                newRecord.parent = prevRecord.offset;
            }

            serializer.UpdateNodeRecord(newRecord);
            NewCheckpointRecord();
            return newRecord;
        }

        public NodeRecord GetNodeRecord(long offset)
        {
            return serializer.GetNodeRecord(offset);
        }

        public bool CanAppendCheckpoint(long checkpointOffset)
        {
            return NextRecord(checkpointOffset) >= globalSave.endCheckpoint ||
                   NextCheckpoint(checkpointOffset) >= globalSave.endCheckpoint;
        }

        public void AppendDialogue(NodeRecord nodeRecord, int dialogueIndex, bool shouldSaveCheckpoint)
        {
            nodeRecord.endDialogue = dialogueIndex + 1;
            if (shouldSaveCheckpoint)
            {
                nodeRecord.lastCheckpointDialogue = dialogueIndex;
            }

            serializer.UpdateNodeRecord(nodeRecord);
        }

        public long AppendCheckpoint(int dialogueIndex, GameStateCheckpoint checkpoint)
        {
            var record = globalSave.endCheckpoint;

            var buf = new ByteSegment(4);
            buf.WriteInt(0, dialogueIndex);
            serializer.AppendRecord(record, buf);
            NewCheckpointRecord();

            serializer.SerializeRecord(globalSave.endCheckpoint, checkpoint);
            NewCheckpointRecord();
            return record;
        }

        public int GetCheckpointDialogue(long offset)
        {
            return serializer.GetRecord(offset).ReadInt(0);
        }

        public GameStateCheckpoint GetCheckpoint(long offset)
        {
            return serializer.DeserializeRecord<GameStateCheckpoint>(serializer.NextRecord(offset));
        }

        #endregion

        #region Checkpoint upgrade

        public Dictionary<string, Differ> CheckScriptUpgrade(ScriptLoader scriptLoader, FlowChartGraph flowChartGraph)
        {
            var changedNode = new Dictionary<string, Differ>();
            if (globalSave.nodeHashes != null)
            {
                foreach (var node in flowChartGraph)
                {
                    if (globalSave.nodeHashes.ContainsKey(node.name) &&
                        globalSave.nodeHashes[node.name] != node.textHash &&
                        reachedDialogues.ContainsKey(node.name))
                    {
                        Debug.Log($"Nova: Node {node.name} needs upgrading.");

                        scriptLoader.AddDeferredDialogueChunks(node);
                        Differ differ = new Differ(node, reachedDialogues[node.name]);
                        differ.GetDiffs();

                        if (differ.distance > 0)
                        {
                            changedNode.Add(node.name, differ);
                        }
                    }
                }

                foreach (var node in globalSave.nodeHashes.Keys)
                {
                    if (!flowChartGraph.HasNode(node))
                    {
                        changedNode.Add(node, null);
                    }
                }
            }

            if (changedNode.Any() || globalSave.nodeHashes == null)
            {
                globalSave.identifier = DateTime.Now.ToBinary();
                globalSave.nodeHashes = flowChartGraph.ToDictionary(node => node.name, node => node.textHash);
                globalSaveDirty = true;
            }

            return changedNode;
        }

        public long UpgradeNodeRecord(NodeRecord nodeRecord, int beginDialogue)
        {
            var beginCheckpoint = GetCheckpoint(NextRecord(nodeRecord.offset));
            beginCheckpoint.dialogueIndex = beginDialogue;

            nodeRecord.beginDialogue = beginDialogue;
            nodeRecord.endDialogue = beginDialogue + 1;
            nodeRecord.lastCheckpointDialogue = beginDialogue;
            nodeRecord.offset = globalSave.endCheckpoint;
            serializer.UpdateNodeRecord(nodeRecord);
            NewCheckpointRecord();

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
                serializer.UpdateNodeRecord(child);
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

        #endregion

        #region Bookmarks

        private string GetBookmarkFileName(int saveID)
        {
            return Path.Combine(savePathBase, $"sav{saveID:D3}.nsav");
        }

        private Bookmark ReplaceCache(int saveID, Bookmark bookmark)
        {
            if (cachedSaveSlots.ContainsKey(saveID))
            {
                var old = cachedSaveSlots[saveID];
                if (old == bookmark)
                {
                    return bookmark;
                }

                Destroy(old.screenshot);
            }

            if (bookmark == null)
            {
                cachedSaveSlots.Remove(saveID);
            }
            else
            {
                cachedSaveSlots[saveID] = bookmark;
            }

            return bookmark;
        }

        public void SaveBookmark(int saveID, Bookmark bookmark, bool cache = true)
        {
            if (cache)
            {
                var screenshot = new Texture2D(bookmark.screenshot.width, bookmark.screenshot.height,
                    bookmark.screenshot.format, false);
                screenshot.SetPixels32(bookmark.screenshot.GetPixels32());
                screenshot.Apply();
                bookmark.screenshot = screenshot;
            }

            bookmark.globalSaveIdentifier = globalSave.identifier;

            serializer.WriteBookmark(GetBookmarkFileName(saveID), cache ? ReplaceCache(saveID, bookmark) : bookmark);
            UpdateGlobalSave();

            var metadata = saveSlotsMetadata.Ensure(saveID);
            metadata.saveID = saveID;
            metadata.modifiedTime = DateTime.Now;
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
            saveSlotsMetadata.Remove(saveID);
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
                if (saveSlotsMetadata.ContainsKey(beginSaveID))
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
                if (!saveSlotsMetadata.ContainsKey(saveID))
                    return null;
                if (!cachedSaveSlots.ContainsKey(saveID))
                    LoadBookmark(saveID);
                return cachedSaveSlots[saveID];
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
            var filtered = saveSlotsMetadata.Values.Where(m => m.saveID >= begin && m.saveID < end).ToList();
            if (!filtered.Any())
                return begin;
            if (type == SaveIDQueryType.Earliest)
                return filtered.Aggregate((agg, val) => agg.modifiedTime < val.modifiedTime ? agg : val).saveID;
            else
                return filtered.Aggregate((agg, val) => agg.modifiedTime > val.modifiedTime ? agg : val).saveID;
        }

        public int QueryMaxSaveID(int begin)
        {
            if (!saveSlotsMetadata.Any())
            {
                return begin;
            }

            return Math.Max(saveSlotsMetadata.Keys.Max(), begin);
        }

        public int QueryMinUnusedSaveID(int begin, int end = int.MaxValue)
        {
            int saveID = begin;
            while (saveID < end && saveSlotsMetadata.ContainsKey(saveID))
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

        #endregion
    }
}
