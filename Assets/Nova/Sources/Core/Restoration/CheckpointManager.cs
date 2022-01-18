using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Assertions;

namespace Nova
{
    using NodeHistory = CountedHashableList<string>;

    #region Classes

    [Serializable]
    public class NodeSaveInfo
    {
        public readonly List<string> nodeHistory;

        public readonly Dictionary<int, GameStateRestoreEntry> restoreEntries =
            new Dictionary<int, GameStateRestoreEntry>();

        public readonly SerializableHashSet<string> reachedBranches = new SerializableHashSet<string>();

        public NodeSaveInfo(List<string> nodeHistory)
        {
            this.nodeHistory = nodeHistory;
        }
    }

    [Serializable]
    public class GlobalSave
    {
        public readonly Dictionary<ulong, NodeSaveInfo> savedNodes = new Dictionary<ulong, NodeSaveInfo>();

        public readonly Dictionary<string, int> reachedWithAnyHistory = new Dictionary<string, int>();

        public readonly Dictionary<string, SerializableHashSet<string>> branchReachedWithAnyHistory =
            new Dictionary<string, SerializableHashSet<string>>();

        public readonly SerializableHashSet<string> reachedEnds = new SerializableHashSet<string>();

        public readonly long globalSaveIdentifier = DateTime.Now.ToBinary();

        /// The global data of the game. For example, the global variables and the unlock status of images and musics.
        /// It is the game author's job to make sure all values are serializable.
        public readonly Dictionary<string, object> data = new Dictionary<string, object>();
    }

    [Serializable]
    public class Bookmark
    {
        public const int ScreenshotWidth = 320;
        public const int ScreenshotHeight = 180;

        public readonly ulong nodeHistoryHash;
        public readonly int dialogueIndex;
        public string description;
        public readonly DateTime creationTime = DateTime.Now;
        public long globalSaveIdentifier;

        private byte[] screenshotBytes;
        [NonSerialized] private Texture2D screenshotTexture;

        public Texture2D screenshot
        {
            get
            {
                if (screenshotBytes == null)
                {
                    Assert.IsTrue(screenshotTexture == null, "Nova: Screenshot cache is not consistent.");
                    return null;
                }

                if (screenshotTexture == null)
                {
                    screenshotTexture = new Texture2D(ScreenshotWidth, ScreenshotHeight, TextureFormat.RGB24, false);
                    screenshotTexture.LoadImage(screenshotBytes);
                }

                return screenshotTexture;
            }
            set
            {
                screenshotTexture = value;
                screenshotBytes = screenshotTexture.EncodeToJPG();
            }
        }

        // NOTE: Do not use default parameters in constructor or it will fail to compile silently...

        /// <summary>
        /// Create a bookmark based on all reached nodes in current gameplay.
        /// </summary>
        /// <param name="nodeHistory">List of all reached nodes, including the current node as the last one.</param>
        /// <param name="dialogueIndex">Index of the current dialogue.</param>
        public Bookmark(NodeHistory nodeHistory, int dialogueIndex)
        {
            nodeHistoryHash = nodeHistory.Hash;
            this.dialogueIndex = dialogueIndex;
        }

        public void TryDestroyTexture()
        {
            if (screenshotTexture)
            {
                UnityEngine.Object.Destroy(screenshotTexture);
            }
        }
    }

    public enum BookmarkType
    {
        AutoSave = 101,
        QuickSave = 201,
        NormalSave = 301
    }

    public enum SaveIDQueryType
    {
        Latest,
        Earliest
    }

    public class BookmarkMetadata
    {
        private int _saveID;

        public int saveID
        {
            get => _saveID;

            set
            {
                type = SaveIDToBookmarkType(value);
                _saveID = value;
            }
        }

        public BookmarkType type { get; private set; }

        public DateTime modifiedTime;

        public static BookmarkType SaveIDToBookmarkType(int saveID)
        {
            if (saveID >= (int)BookmarkType.NormalSave)
            {
                return BookmarkType.NormalSave;
            }

            if (saveID >= (int)BookmarkType.QuickSave)
            {
                return BookmarkType.QuickSave;
            }

            return BookmarkType.AutoSave;
        }
    }

    #endregion

    /// <summary>
    /// Manager component providing ability to manage the game progress and save files.
    /// </summary>
    public class CheckpointManager : MonoBehaviour
    {
        public string saveFolder;
        private string savePathBase;
        private string globalSavePath;

        private GlobalSave globalSave;

        private readonly Dictionary<int, Bookmark> cachedSaveSlots = new Dictionary<int, Bookmark>();
        public readonly Dictionary<int, BookmarkMetadata> saveSlotsMetadata = new Dictionary<int, BookmarkMetadata>();

        private readonly CheckpointSerializer serializer = new CheckpointSerializer();

        private void Start()
        {
            savePathBase = Path.Combine(Application.persistentDataPath, "Save", saveFolder);
            globalSavePath = Path.Combine(savePathBase, "global.nsav");
            Directory.CreateDirectory(savePathBase);

            if (File.Exists(globalSavePath))
            {
                try
                {
                    globalSave = serializer.SafeRead<GlobalSave>(globalSavePath);
                }
                catch (Exception ex)
                {
                    Debug.LogErrorFormat("Nova: Cannot load global save file: {0}", ex.Message);
                    Alert.Show(
                        null,
                        I18n.__("bookmark.load.globalfail"),
                        ResetGlobalSave,
                        Utils.Quit
                    );
                }
            }
            else
            {
                ResetGlobalSave();
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
        }

        private void OnDestroy()
        {
            UpdateGlobalSave();

            foreach (var bookmark in cachedSaveSlots.Values)
            {
                bookmark.TryDestroyTexture();
            }
        }

        #region Global save

        private NodeSaveInfo EnsureNodeHistory(NodeHistory nodeHistory)
        {
            if (globalSave.savedNodes.TryGetValue(nodeHistory.Hash, out var info))
            {
                return info;
            }

            info = new NodeSaveInfo(nodeHistory.Select(x => x.Key).ToList());
            globalSave.savedNodes[nodeHistory.Hash] = info;
            return info;
        }

        public List<string> GetNodeHistory(ulong nodeHistoryHash)
        {
            if (globalSave.savedNodes.TryGetValue(nodeHistoryHash, out var info))
            {
                return info.nodeHistory;
            }

            return null;
        }

        /// <summary>
        /// Set a dialogue to "reached" state and save the restore entry for the dialogue.
        /// </summary>
        /// <param name="nodeHistory">The list of all reached nodes.</param>
        /// <param name="dialogueIndex">The index of the dialogue.</param>
        /// <param name="entry">Restore entry for the dialogue</param>
        public void SetReached(NodeHistory nodeHistory, int dialogueIndex, GameStateRestoreEntry entry)
        {
            EnsureNodeHistory(nodeHistory).restoreEntries[dialogueIndex] = entry;

            var nodeName = nodeHistory.Last().Key;
            var oldIndex = globalSave.reachedWithAnyHistory.Ensure(nodeName);
            globalSave.reachedWithAnyHistory[nodeName] = Math.Max(oldIndex, dialogueIndex);
        }

        /// <summary>
        /// Set a branch to "reached" state.
        /// </summary>
        /// <param name="nodeHistory">The list of all reached nodes.</param>
        /// <param name="branchName">The name of the branch.</param>
        public void SetBranchReached(NodeHistory nodeHistory, string branchName)
        {
            EnsureNodeHistory(nodeHistory).reachedBranches.Add(branchName);

            var nodeName = nodeHistory.Last().Key;
            var branchNames = globalSave.branchReachedWithAnyHistory.Ensure(nodeName);
            branchNames.Add(branchName);
        }

        /// <summary>
        /// Set an end point to "reached" state.
        /// </summary>
        /// <param name="endName">The name of the end point.</param>
        public void SetEndReached(string endName)
        {
            globalSave.reachedEnds.Add(endName);
        }

        public void UnsetReached(ulong nodeHistoryHash)
        {
            globalSave.savedNodes.Remove(nodeHistoryHash);
        }

        public void UnsetReached(NodeHistory nodeHistory, int dialogueIndex)
        {
            if (globalSave.savedNodes.TryGetValue(nodeHistory.Hash, out var info))
            {
                info.restoreEntries.Remove(dialogueIndex);
            }
        }

        public GameStateRestoreEntry GetReached(ulong nodeHistoryHash, int dialogueIndex)
        {
            if (globalSave.savedNodes.TryGetValue(nodeHistoryHash, out var info)
                && info.restoreEntries.TryGetValue(dialogueIndex, out var entry))
            {
                return entry;
            }

            return null;
        }

        /// <summary>
        /// Get the restore entry for a dialogue.
        /// </summary>
        /// <param name="nodeHistory">The list of all reached nodes.</param>
        /// <param name="dialogueIndex">The index of the dialogue.</param>
        /// <returns>The restore entry for the dialogue. Null if not reached.</returns>
        public GameStateRestoreEntry GetReached(NodeHistory nodeHistory, int dialogueIndex)
        {
            return GetReached(nodeHistory.Hash, dialogueIndex);
        }

        public bool IsReachedWithAnyHistory(string nodeName, int dialogueIndex)
        {
            return globalSave.reachedWithAnyHistory.TryGetValue(nodeName, out var oldIndex)
                   && oldIndex >= dialogueIndex;
        }

        /// <summary>
        /// Check if the branch has been reached.
        /// </summary>
        /// <param name="nodeHistory">The list of all reached nodes.</param>
        /// <param name="branchName">The name of the branch.</param>
        /// <returns>Whether the branch has been reached.</returns>
        public bool IsBranchReached(NodeHistory nodeHistory, string branchName)
        {
            return globalSave.savedNodes.TryGetValue(nodeHistory.Hash, out var info)
                   && info.reachedBranches.Contains(branchName);
        }

        public bool IsBranchReachedWithAnyHistory(string nodeName, string branchName)
        {
            return globalSave.branchReachedWithAnyHistory.TryGetValue(nodeName, out var branchNames)
                   && branchNames.Contains(branchName);
        }

        /// <summary>
        /// Check if the end point has been reached.
        /// </summary>
        /// <param name="endName">The name of the end point.</param>
        /// <returns>Whether the end point has been reached.</returns>
        public bool IsEndReached(string endName)
        {
            return globalSave.reachedEnds.Contains(endName);
        }

        /// <summary>
        /// Update the global save file.
        /// </summary>
        /// TODO: UpdateGlobalSave() is slow when there are many saved dialogue entries
        public void UpdateGlobalSave()
        {
            serializer.SafeWrite(globalSave, globalSavePath);
        }

        /// <summary>
        /// Reset the global save file to clear all progress.
        /// Note that all bookmarks will be invalid.
        /// </summary>
        public void ResetGlobalSave()
        {
            var saveDir = new DirectoryInfo(savePathBase);
            foreach (var file in saveDir.GetFiles())
                file.Delete();

            globalSave = new GlobalSave();
            serializer.SafeWrite(globalSave, globalSavePath);
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
            bookmark.globalSaveIdentifier = globalSave.globalSaveIdentifier;

            serializer.SafeWrite(ReplaceCache(saveID, bookmark), GetBookmarkFileName(saveID));
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
            var bookmark = serializer.SafeRead<Bookmark>(GetBookmarkFileName(saveID), result =>
            {
                this.RuntimeAssert(result.globalSaveIdentifier == globalSave.globalSaveIdentifier,
                    "Save file is incompatible with the global save file.");
            });
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
        /// Get the stored global flag
        /// </summary>
        /// <see cref="GlobalSave"/>
        /// <param name="key">the key of the global flag</param>
        /// <param name="defaultValue">
        /// the default value if the value is not found or cannot be converted to the target type T
        /// </param>
        /// <typeparam name="T">
        /// the type of value
        /// </typeparam>
        /// <returns>the value of the flag</returns>
        public T Get<T>(string key, T defaultValue = default)
        {
            if (!globalSave.data.TryGetValue(key, out object value))
            {
                return defaultValue;
            }

            if (value is T tValue)
            {
                return tValue;
            }

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (InvalidCastException)
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Set global flag to the given value
        /// </summary>
        /// <see cref="GlobalSave"/>
        /// <param name="key">
        /// the key of the flag
        /// </param>
        /// <param name="value">
        /// the value of the flag
        /// </param>
        public void Set(string key, object value)
        {
            globalSave.data[key] = value;
        }

        #endregion
    }
}