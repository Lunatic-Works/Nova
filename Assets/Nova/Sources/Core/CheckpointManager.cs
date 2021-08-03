using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Assertions;

namespace Nova
{
    #region Classes

    /// <summary>
    /// Containing progress and snapshots of reached FlowChartNode.
    /// </summary>
    [Serializable]
    public class NodeSaveInfo
    {
        public readonly Dictionary<int, GameStateRestoreEntry> dialogueRestoreEntries;
        public readonly SerializableHashSet<string> reachedBranches;

        public NodeSaveInfo()
        {
            dialogueRestoreEntries = new Dictionary<int, GameStateRestoreEntry>();
            reachedBranches = new SerializableHashSet<string>();
        }
    }

    /// <summary>
    /// Global save file. Containing progress of FlowChartTree.
    /// </summary>
    [Serializable]
    public class GlobalSave
    {
        public readonly Dictionary<ulong, Dictionary<string, NodeSaveInfo>> savedNodesByVariablesHash =
            new Dictionary<ulong, Dictionary<string, NodeSaveInfo>>();

        public readonly SerializableHashSet<string> reachedEnds = new SerializableHashSet<string>();
        public readonly long globalSaveIdentifier = DateTime.Now.ToBinary();

        /// The global flags and status of the game. For example, the unlock status of music or CG
        /// It is the game author's job to make sure all values are serializable
        public readonly Dictionary<string, object> data = new Dictionary<string, object>();
    }

    /// <summary>
    /// Individual save file.
    /// Acting as a "Bookmark" recording current FlowChartNode and index of dialogue.
    /// </summary>
    [Serializable]
    public class Bookmark
    {
        public const int ScreenshotWidth = 320;
        public const int ScreenshotHeight = 180;

        private static readonly byte[] JPEGHeader = {0xFF, 0xD8, 0xFF, 0xE0};

        public readonly List<string> nodeHistory;
        public readonly int dialogueIndex;
        public readonly string description;
        public readonly ulong variablesHash;
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

                    if (screenshotBytes.Take(JPEGHeader.Length).SequenceEqual(JPEGHeader))
                    {
                        screenshotTexture.LoadImage(screenshotBytes);
                    }
                    else
                    {
                        screenshotTexture.LoadRawTextureData(screenshotBytes);
                        screenshotTexture.Apply();
                    }
                }

                return screenshotTexture;
            }
            set
            {
                screenshotTexture = value;
                screenshotBytes = screenshotTexture.EncodeToJPG();
            }
        }

        // NOTE: Do not use default parameter in constructor or it will fail to compile silently...

        /// <summary>
        /// Create a bookmark based on all reached nodes in current gameplay.
        /// </summary>
        /// <param name="nodeHistory">List of all reached nodes, including the current one as the last node.</param>
        /// <param name="dialogueIndex">Index of the current dialogue.</param>
        /// <param name="description">Description of this bookmark.</param>
        /// <param name="variablesHash">Variables hash of current bookmark.</param>
        public Bookmark(List<string> nodeHistory, int dialogueIndex, string description, ulong variablesHash)
        {
            this.nodeHistory = new List<string>(nodeHistory);
            this.dialogueIndex = dialogueIndex;
            this.description = description;
            this.variablesHash = variablesHash;
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
        private const int Version = 2;

        private string savePathBase;
        private string globalSavePath;
        private byte[] fileHeader;

        private BinaryFormatter formatter;

        private GlobalSave globalSave;

        private Dictionary<int, Bookmark> cachedSaveSlots;
        public string saveFolder = "";
        public Dictionary<int, BookmarkMetadata> saveSlotsMetadata { get; private set; }

        [HideInInspector] public GameStateCheckpoint clearSceneRestoreEntry;

        /// <summary>
        /// Initialization of members which are unlikely to change in the future
        /// </summary>
        public void InitVariables()
        {
            clearSceneRestoreEntry = null;
            savePathBase = Path.Combine(Application.persistentDataPath, "Save", saveFolder);
            globalSavePath = Path.Combine(savePathBase, "global.nsav");
            fileHeader = Encoding.ASCII.GetBytes("NOVASAVE");
            formatter = new BinaryFormatter();
            cachedSaveSlots = new Dictionary<int, Bookmark>();
            saveSlotsMetadata = new Dictionary<int, BookmarkMetadata>();
        }

        private void Start()
        {
            InitVariables();

            Directory.CreateDirectory(savePathBase);

            if (File.Exists(globalSavePath))
            {
                try
                {
                    globalSave = SafeRead<GlobalSave>(globalSavePath);
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

        /// <summary>
        /// Set a dialogue to "reached" state and save the restore entry for the dialogue.
        /// </summary>
        /// <param name="nodeName">The name of FlowChartNode containing the dialogue.</param>
        /// <param name="dialogueIndex">The index of the dialogue.</param>
        /// <param name="variables">Current variables.</param>
        /// <param name="entry">Restore entry for the dialogue</param>
        public void SetReached(string nodeName, int dialogueIndex, Variables variables, GameStateRestoreEntry entry)
        {
            globalSave.savedNodesByVariablesHash.Ensure(variables.hash).Ensure(nodeName)
                .dialogueRestoreEntries[dialogueIndex] = entry;
        }

        /// <summary>
        /// Set a branch to "reached" state.
        /// </summary>
        /// <param name="nodeName">The name of FlowChartNode containing the branch.</param>
        /// <param name="branchName">The name of the branch.</param>
        /// <param name="variables">Current variables.</param>
        public void SetReached(string nodeName, string branchName, Variables variables)
        {
            globalSave.savedNodesByVariablesHash.Ensure(variables.hash).Ensure(nodeName).reachedBranches
                .Add(branchName);
        }

        /// <summary>
        /// Set an end point to "reached" state.
        /// </summary>
        /// <param name="endName">The name of the end point.</param>
        public void SetReached(string endName)
        {
            globalSave.reachedEnds.Add(endName);
        }

        // DEBUG ONLY METHOD: will destroy all versions regardless of variables
        public void UnsetReached(string nodeName, int dialogueIndex)
        {
            foreach (var dict in globalSave.savedNodesByVariablesHash.Values)
            {
                if (dict.TryGetValue(nodeName, out NodeSaveInfo nodeInfo))
                    nodeInfo.dialogueRestoreEntries.Remove(dialogueIndex);
            }
        }

        // DEBUG ONLY METHOD: will destroy all versions regardless of variables
        public void UnsetReached(string nodeName)
        {
            foreach (var dict in globalSave.savedNodesByVariablesHash.Values)
            {
                dict.Remove(nodeName);
            }
        }

        /// <summary>
        /// Check if the dialogue has been reached with any combination of variables.
        /// </summary>
        /// <param name="nodeName">The name of FlowChartNode containing the dialogue.</param>
        /// <param name="dialogueIndex">The index of the dialogue.</param>
        /// <returns>The restore entry for the dialogue. Null if not reached.</returns>
        public GameStateRestoreEntry GetReachedForAnyVariables(string nodeName, int dialogueIndex)
        {
            // If reading global save file fails, globalSave.savedNodesByVariablesHash will be null
            if (globalSave?.savedNodesByVariablesHash == null)
            {
                return null;
            }

            foreach (var dict in globalSave.savedNodesByVariablesHash.Values)
            {
                if (dict.TryGetValue(nodeName, out NodeSaveInfo info) &&
                    info.dialogueRestoreEntries.TryGetValue(dialogueIndex, out GameStateRestoreEntry entry))
                    return entry;
            }

            return null;
        }

        /// <summary>
        /// Check if the dialogue has been reached and retrieve the restore entry.
        /// </summary>
        /// <param name="nodeName">The name of FlowChartNode containing the dialogue.</param>
        /// <param name="dialogueIndex">The index of the dialogue.</param>
        /// <param name="variablesHash">Hash of current variables.</param>
        /// <returns>The restore entry for the dialogue. Null if not reached.</returns>
        public GameStateRestoreEntry GetReached(string nodeName, int dialogueIndex, ulong variablesHash)
        {
            if (globalSave.savedNodesByVariablesHash.Ensure(variablesHash).TryGetValue(nodeName, out NodeSaveInfo info))
                if (info.dialogueRestoreEntries.TryGetValue(dialogueIndex, out GameStateRestoreEntry entry))
                    return entry;
            return null;
        }

        /// <summary>
        /// Check if the branch has been reached.
        /// </summary>
        /// <param name="nodeName">The name of FlowChartNode containing the branch.</param>
        /// <param name="branchName">The name of the branch.</param>
        /// <param name="variablesHash">Hash of current variables.</param>
        /// <returns>Whether the branch has been reached.</returns>
        public bool IsReached(string nodeName, string branchName, ulong variablesHash)
        {
            if (globalSave.savedNodesByVariablesHash.Ensure(variablesHash).TryGetValue(nodeName, out NodeSaveInfo info))
                return info.reachedBranches.Contains(branchName);
            return false;
        }

        /// <summary>
        /// Check if the end point has been reached.
        /// </summary>
        /// <param name="endName">The name of the end point.</param>
        /// <returns>Whether the end point has been reached.</returns>
        public bool IsReached(string endName)
        {
            return globalSave.reachedEnds.Contains(endName);
        }

        /// <summary>
        /// Update the global save file.
        /// </summary>
        /// TODO: UpdateGlobalSave() is slow when there are many saved dialogue entries
        public void UpdateGlobalSave()
        {
            SafeWrite(globalSave, globalSavePath);
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
            using (var fs = File.OpenWrite(globalSavePath))
                WriteSave(globalSave, fs);
        }

        #endregion

        #region Read and write

        private T SafeRead<T>(string path)
        {
            return SafeRead<T>(path, _ => { });
        }

        private T SafeRead<T>(string path, Action<T> assertion)
        {
            try
            {
                try
                {
                    using (var fs = File.OpenRead(path))
                    {
                        var result = ReadSave<T>(fs);
                        assertion(result);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Nova: {path} is corrupted, details below. Try to recover");
                    Debug.LogWarning(ex.Message);

                    var oldPath = path + ".old";
                    using (var fs = File.OpenRead(oldPath))
                    {
                        var result = ReadSave<T>(fs);
                        assertion(result);

                        // Recover only if the old file is good
                        File.Delete(path); // no exception if not exist
                        File.Move(oldPath, path); // exception if exists

                        return result;
                    }
                }
            }
            catch (Exception)
            {
                Debug.LogError($"Nova: Error loading {path}, details below");
                throw; // Nested exception cannot display full message here
            }
        }

        private bool alertOnSafeWriteFail = true;

        private void SafeWrite<T>(T obj, string path)
        {
            try
            {
                var oldPath = path + ".old";
                if (File.Exists(path))
                {
                    File.Delete(oldPath); // no exception if not exist
                    File.Move(path, oldPath); // exception if exists
                }

                using (var fs = File.OpenWrite(path)) // overwrite if needed
                    WriteSave(obj, fs); // May be interrupted

                File.Delete(oldPath);
            }
            catch (Exception ex)
            {
                // If there is some problem with Alert.Show, we need to avoid infinite recursion
                if (alertOnSafeWriteFail)
                {
                    alertOnSafeWriteFail = false;
                    Alert.Show(I18n.__("bookmark.save.fail"), ex.Message);
                }
                throw;
            }
        }

        private T ReadSave<T>(Stream s)
        {
            using (var bw = new BinaryReader(s))
            {
                this.RuntimeAssert(fileHeader.SequenceEqual(bw.ReadBytes(fileHeader.Length)),
                    "Invalid save file format.");

                int version = bw.ReadInt32();
                this.RuntimeAssert(Version >= version,
                    "Save file is incompatible with the current version of Nova.");

                if (version == 2)
                {
                    using (var compressed = new DeflateStream(s, CompressionMode.Decompress))
                    using (var uncompressed = new MemoryStream())
                    {
                        compressed.CopyTo(uncompressed);
                        uncompressed.Position = 0;
                        return (T)formatter.Deserialize(uncompressed);
                    }
                }
                else // version == 1
                {
                    return (T)formatter.Deserialize(new XorStream(s, fileHeader));
                }
            }
        }

        private void WriteSave<T>(T obj, Stream s)
        {
            using (var bw = new BinaryWriter(s))
            {
                bw.Write(fileHeader);
                bw.Write(Version);

                using (var compressed = new DeflateStream(s, CompressionMode.Compress))
                using (var uncompressed = new MemoryStream())
                {
                    formatter.Serialize(uncompressed, obj);
                    uncompressed.Position = 0;
                    uncompressed.CopyTo(compressed);
                }
            }
        }

        #endregion

        #region Bookmarks

        private string ComposeFileName(int saveID)
        {
            return Path.Combine(savePathBase, $"sav{saveID:D3}.nsav");
        }

        private Bookmark ReplaceCache(int saveID, Bookmark newBookmark)
        {
            if (cachedSaveSlots.ContainsKey(saveID))
            {
                var old = cachedSaveSlots[saveID];
                if (old == newBookmark)
                {
                    return newBookmark;
                }

                Destroy(old.screenshot);
            }

            if (newBookmark == null)
            {
                cachedSaveSlots.Remove(saveID);
            }
            else
            {
                cachedSaveSlots[saveID] = newBookmark;
            }

            return newBookmark;
        }

        /// <summary>
        /// Save a bookmark to disk, and update the global save file.
        /// Will throw exception if it fails.
        /// </summary>
        /// <param name="saveID">ID of the bookmark.</param>
        /// <param name="save">The bookmark to save.</param>
        public void SaveBookmark(int saveID, Bookmark save)
        {
            var screenshotClone = new Texture2D(save.screenshot.width, save.screenshot.height, save.screenshot.format,
                false);
            screenshotClone.SetPixels32(save.screenshot.GetPixels32());
            screenshotClone.Apply();
            save.screenshot = screenshotClone;
            save.globalSaveIdentifier = globalSave.globalSaveIdentifier;
            SafeWrite(ReplaceCache(saveID, save), ComposeFileName(saveID));
            UpdateGlobalSave();

            saveSlotsMetadata.Ensure(saveID).saveID = saveID;
            saveSlotsMetadata.Ensure(saveID).modifiedTime = DateTime.Now;
        }

        /// <summary>
        /// Load a bookmark from disk. Never uses cache.
        /// Will throw exception if it fails.
        /// </summary>
        /// <param name="saveID">ID of the bookmark.</param>
        /// <returns>The loaded bookmark.</returns>
        public Bookmark LoadBookmark(int saveID)
        {
            return ReplaceCache(saveID, SafeRead<Bookmark>(ComposeFileName(saveID), result =>
            {
                this.RuntimeAssert(result.globalSaveIdentifier == globalSave.globalSaveIdentifier,
                    "Save file is incompatible with the global save file.");
            }));
        }

        /// <summary>
        /// Delete a specified bookmark.
        /// </summary>
        /// <param name="saveID">ID of the bookmark.</param>
        public void DeleteBookmark(int saveID)
        {
            File.Delete(ComposeFileName(saveID));
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