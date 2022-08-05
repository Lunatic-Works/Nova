using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Nova
{
    [Serializable]
    public class OldGlobalSave
    {
        // Node name -> dialogue index -> node history hash -> GameStateRestoreEntry
        // TODO: deduplicate restoreDatas
        public readonly Dictionary<string, Dictionary<int, Dictionary<ulong, GameStateRestoreEntry>>> reachedDialogues =
            new Dictionary<string, Dictionary<int, Dictionary<ulong, GameStateRestoreEntry>>>();

        // Node name -> branch name -> node history hash -> bool
        public readonly Dictionary<string, Dictionary<string, SerializableHashSet<ulong>>> reachedBranches =
            new Dictionary<string, Dictionary<string, SerializableHashSet<ulong>>>();

        // End name -> bool
        public readonly SerializableHashSet<string> reachedEnds = new SerializableHashSet<string>();

        // Node history hash -> NodeHistory
        // TODO: use a radix tree to store node histories
        public readonly Dictionary<ulong, NodeHistoryData> cachedNodeHistories =
            new Dictionary<ulong, NodeHistoryData>();

        public readonly long identifier = DateTime.Now.ToBinary();

        /// The global data of the game. For example, the global variables and the unlock status of images and musics.
        /// It is the game author's job to make sure all values are serializable.
        public readonly Dictionary<string, object> data = new Dictionary<string, object>();
    }

    /// <summary>
    /// Manager component providing ability to manage the game progress and save files.
    /// </summary>
    public class CheckpointManager : MonoBehaviour
    {
        public string saveFolder;
        private string savePathBase;
        private string globalSavePath;

        private GlobalSave globalSave;
        private bool globalSaveDirty = false;
        private readonly Dictionary<ReachedDialogueKey, ReachedDialogueData> reachedDialogues = new Dictionary<ReachedDialogueKey, ReachedDialogueData>();
        private readonly SerializableHashSet<string> reachedEnds = new SerializableHashSet<string>();

        private readonly Dictionary<int, Bookmark> cachedSaveSlots = new Dictionary<int, Bookmark>();
        public readonly Dictionary<int, BookmarkMetadata> saveSlotsMetadata = new Dictionary<int, BookmarkMetadata>();

        private CheckpointSerializer serializer;

        private bool inited;

        private void InitGlobalSave()
        {
            globalSave = serializer.DeserializeRecord<GlobalSave>(CheckpointSerializer.GlobalSaveOffset);
            if (globalSave.version != CheckpointSerializer.Version ||
                !CheckpointSerializer.FileHeader.SequenceEqual(globalSave.fileHeader))
            {
                throw CheckpointCorruptedException.BadHeader;
            }
        }

        private void InitReached()
        {
            reachedDialogues.Clear();
            reachedEnds.Clear();
            for (var cur = globalSave.beginReached; cur < globalSave.endReached; cur = serializer.NextRecord(cur))
            {
                var record = serializer.DeserializeRecord(cur);
                if (record is string endName)
                {
                    reachedEnds.Add(endName);
                }
                else if (record is ReachedDialogueData dialogue)
                {
                    reachedDialogues.Add(new ReachedDialogueKey(dialogue), dialogue);
                }
            }
        }

        // Should be called in Start, not in Awake
        public void Init()
        {
            if (inited)
            {
                return;
            }

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

        private void Start()
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

        #region Global save

        private void NewReached()
        {
            globalSave.endReached = serializer.NextRecord(globalSave.endReached);
            globalSaveDirty = true;
            Debug.Log($"next reached {globalSave.endReached}");
        }

        private void NewCheckpoint()
        {
            globalSave.endCheckpoint = serializer.NextRecord(globalSave.endCheckpoint);
            globalSaveDirty = true;
            Debug.Log($"next checkpoint {globalSave.endCheckpoint}");
        }

        public long NextRecord(long offset)
        {
            return serializer.NextRecord(offset);
        }

        public long NextCheckpoint(long offset)
        {
            return NextRecord(NextRecord(offset));
        }

        public NodeRecord GetNextNode(NodeRecord prevRecord, string name, Variables variables, int beginDialogue)
        {
            var variableHash = variables.hash;
            NodeRecord record = null;
            var offset = prevRecord == null ? globalSave.beginCheckpoint : prevRecord.child;
            while (offset != 0 && offset < globalSave.endCheckpoint)
            {
                record = serializer.GetNodeRecord(offset);
                if (record.name == name && record.variableHash == variableHash)
                {
                    return record;
                }
                offset = record.brother;
            }
            offset = globalSave.endCheckpoint;
            var newRecord = new NodeRecord(offset, name, beginDialogue, variableHash);
            if (record != null)
            {
                record.brother = offset;
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
            NewCheckpoint();
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

            var buf = new ByteSegment(new byte[4]);
            buf.WriteInt(0, dialogueIndex);
            serializer.AppendRecord(record, buf);
            NewCheckpoint();

            serializer.SerializeRecord(globalSave.endCheckpoint, checkpoint, true);
            NewCheckpoint();
            return record;
        }

        public int GetCheckpointDialogue(long offset)
        {
            return serializer.GetRecord(offset).ReadInt(0);
        }

        public GameStateCheckpoint GetCheckpoint(long offset)
        {
            return serializer.DeserializeRecord<GameStateCheckpoint>(serializer.NextRecord(offset), true);
        }

        /// <summary>
        /// Set a dialogue to "reached" state and save the restore entry for the dialogue.
        /// </summary>
        /// <param name="nodeHistory">The list of all reached nodes.</param>
        /// <param name="dialogueIndex">The index of the dialogue.</param>
        /// <param name="entry">Restore entry for the dialogue.</param>
        public void SetReached(ReachedDialogueData data)
        {
            var key = new ReachedDialogueKey(data);
            if (reachedDialogues.ContainsKey(key))
            {
                return;
            }
            reachedDialogues.Add(key, data);
            serializer.SerializeRecord(globalSave.endReached, data);
            NewReached();
        }

        /// <summary>
        /// Set a branch to "reached" state.
        /// </summary>
        /// <param name="nodeHistory">The list of all reached nodes.</param>
        /// <param name="branchName">The name of the branch.</param>
        public void SetBranchReached(NodeRecord nodeRecord, string branchName)
        {
            // currently we cannot find next node by branchname
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set an end point to "reached" state.
        /// </summary>
        /// <param name="endName">The name of the end point.</param>
        public void SetEndReached(string endName)
        {
            if (reachedEnds.Contains(endName))
            {
                return;
            }
            reachedEnds.Add(endName);
            serializer.SerializeRecord(globalSave.endReached, endName);
            NewReached();
        }

        public bool IsReachedAnyHistory(string nodeName, int dialogueIndex)
        {
            return reachedDialogues.ContainsKey(new ReachedDialogueKey(nodeName, dialogueIndex));
        }

        public ReachedDialogueData GetReachedDialogueData(string nodeName, int dialogueIndex)
        {
            return reachedDialogues[new ReachedDialogueKey(nodeName, dialogueIndex)];
        }

        /// <summary>
        /// Check if the branch has been reached.
        /// </summary>
        /// <param name="nodeHistory">The list of all reached nodes.</param>
        /// <param name="branchName">The name of the branch.</param>
        /// <returns>Whether the branch has been reached.</returns>
        public bool IsBranchReached(NodeRecord nodeRecord, string nextNodeName)
        {
            throw new NotImplementedException();
        }

        public bool IsBranchReachedAnyHistory(string nodeName, string nextNodeName)
        {
            // currently we don't store this
            throw new NotImplementedException();
        }

        /// <summary>
        /// Check if the end point has been reached.
        /// </summary>
        /// <param name="endName">The name of the end point.</param>
        /// <returns>Whether the end point has been reached.</returns>
        public bool IsEndReached(string endName)
        {
            return reachedEnds.Contains(endName);
        }

        /// <summary>
        /// Update the global save file.
        /// </summary>
        /// TODO: UpdateGlobalSave() is slow when there are many saved dialogue entries
        public void UpdateGlobalSave()
        {
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
        public void ResetGlobalSave()
        {
            var saveDir = new DirectoryInfo(savePathBase);
            foreach (var file in saveDir.GetFiles())
            {
                file.Delete();
            }

            serializer.Open();
            globalSave = new GlobalSave(serializer);
            globalSaveDirty = true;
            UpdateGlobalSave();
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

        /// <summary>
        /// Save a bookmark to disk, and update the global save file.
        /// Will throw exception if it fails.
        /// </summary>
        /// <param name="saveID">ID of the bookmark.</param>
        /// <param name="bookmark">The bookmark to save.</param>
        public void SaveBookmark(int saveID, Bookmark bookmark)
        {
            var screenshot = new Texture2D(bookmark.screenshot.width, bookmark.screenshot.height,
                bookmark.screenshot.format, false);
            screenshot.SetPixels32(bookmark.screenshot.GetPixels32());
            screenshot.Apply();
            bookmark.screenshot = screenshot;
            bookmark.globalSaveIdentifier = globalSave.identifier;

            serializer.WriteBookmark(GetBookmarkFileName(saveID), ReplaceCache(saveID, bookmark));
            UpdateGlobalSave();

            var metadata = saveSlotsMetadata.Ensure(saveID);
            metadata.saveID = saveID;
            metadata.modifiedTime = DateTime.Now;
        }

        /// <summary>
        /// Load a bookmark from disk. Never uses cache.
        /// Will throw exception if it fails.
        /// </summary>
        /// <param name="saveID">ID of the bookmark.</param>
        /// <returns>The loaded bookmark.</returns>
        public Bookmark LoadBookmark(int saveID)
        {
            var bookmark = serializer.ReadBookmark(GetBookmarkFileName(saveID));
            if (bookmark.globalSaveIdentifier != globalSave.identifier)
            {
                Debug.LogWarning($"Nova: Save file is incompatible with the global save file. saveID: {saveID}");
                bookmark = null;
            }

            return ReplaceCache(saveID, bookmark);
        }

        /// <summary>
        /// Delete a specified bookmark.
        /// </summary>
        /// <param name="saveID">ID of the bookmark.</param>
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

        public int QueryMinUnusedSaveID(int begin, int end)
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
