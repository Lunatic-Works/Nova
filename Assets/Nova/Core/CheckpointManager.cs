using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Assertions;

namespace Nova
{
    #region SerializeUtil

    /// <summary>
    /// Implementation of Serializable HashSet.
    /// The original HashSet is not serializable.
    /// </summary>
    /// <typeparam name="T">Type of value in HashSet</typeparam>
    [Serializable]
    public class SerializableHashSet<T> : HashSet<T>
    {
        public SerializableHashSet()
        {
        }

        protected SerializableHashSet(SerializationInfo info, StreamingContext context)
        {
            foreach (var val in (List<T>) info.GetValue("values", typeof(List<T>)))
                Add(val);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("values", this.ToList());
        }
    }

    #endregion

    #region SaveTypes

    /// <summary>
    /// Containing progress and snapshots of reached FlowChartNode.
    /// </summary>
    [Serializable]
    public class NodeSaveInfo
    {
        public readonly Dictionary<int, GameStateStepRestoreEntry> DialogueRestoreEntries;
        public readonly SerializableHashSet<string> ReachedBranches;

        public NodeSaveInfo()
        {
            DialogueRestoreEntries = new Dictionary<int, GameStateStepRestoreEntry>();
            ReachedBranches = new SerializableHashSet<string>();
        }
    }

    /// <summary>
    /// Global save file. Containing progress of FlowChartTree.
    /// </summary>
    [Serializable]
    public class GlobalSave
    {
        public readonly Dictionary<string, NodeSaveInfo> SavedNodes = new Dictionary<string, NodeSaveInfo>();
        public readonly SerializableHashSet<string> ReachedEndings = new SerializableHashSet<string>();
        public readonly long GlobalSaveIdentifier = DateTime.Now.ToBinary();
    }

    /// <summary>
    /// Individual save file.
    /// Acting as a "Bookmark" recording current FlowChartNode and index of dialogue.
    /// </summary>
    [Serializable]
    public class Bookmark
    {
        public const int ScreenShotWidth = 320;
        public const int ScreenShotHeight = 180;

        public readonly int DialogueIndex;
        public readonly List<string> NodeHistory;
        public long GlobalSaveIdentifier;
        public readonly DateTime CreationTime = DateTime.Now;
        public string Description;

        private byte[] _screenShotBytes;
        [NonSerialized] private Texture2D _screenShotTexture;
        public Texture2D ScreenShot
        {
            get
            {
                if (_screenShotBytes == null)
                {
                    Assert.IsTrue(_screenShotTexture == null, "Nova: ScreenShot cache is not consistent");
                    return null;
                }

                if (_screenShotTexture == null)
                {
                    _screenShotTexture = new Texture2D(ScreenShotWidth, ScreenShotHeight, TextureFormat.RGB24, false);
                    _screenShotTexture.LoadRawTextureData(_screenShotBytes);
                    _screenShotTexture.Apply();
                }

                return _screenShotTexture;
            }
            set
            {
                _screenShotTexture = value;
                _screenShotBytes = _screenShotTexture.GetRawTextureData();
            }
        }

        // NOTE: Do not use default parameter in constructor or it will fail to compile silently...

        /// <summary>
        /// Create a bookmark based on all reached nodes in current gameplay.
        /// </summary>
        /// <param name="nodeHistory">List of all reached nodes, including the current one as the last node.</param>
        /// <param name="dialogueIndex">Index of the current dialogue.</param>
        /// <param name="description">Description of this bookmark.</param>
        public Bookmark(List<string> nodeHistory, int dialogueIndex, string description)
        {
            NodeHistory = new List<string>(nodeHistory);
            DialogueIndex = dialogueIndex;
            Description = description;
        }
    }

    #endregion

    public enum BookmarkType
    {
        AutoSave = 101,
        QuickSave = 201,
        NormalSave = 301
    }

    public enum SaveIdQueryType
    {
        Latest,
        Earliest
    }

    public class BookmarkMetadata
    {
        public int SaveId
        {
            get { return _saveId; }

            set
            {
                Type = SaveIdToBookmarkType(value);
                _saveId = value;
            }
        }
        public BookmarkType Type { get; private set; }
        public DateTime ModifiedTime;

        private int _saveId;

        public static BookmarkType SaveIdToBookmarkType(int saveId)
        {
            if (saveId >= (int)BookmarkType.NormalSave)
            {
                return BookmarkType.NormalSave;
            }
            if (saveId >= (int)BookmarkType.QuickSave)
            {
                return BookmarkType.QuickSave;
            }
            return BookmarkType.AutoSave;
        }
    }

    /// <summary>
    /// Manager component providing ability to manage game progress and save files.
    /// </summary>
    public class CheckpointManager : MonoBehaviour
    {
        private const int Version = 1;

        private string _savePathBase;
        private string _globalSavePath;
        private byte[] _fileHeader;

        private DESCryptoServiceProvider _cryptic;
        private BinaryFormatter _formatter;

        private GlobalSave _globalSave;

        private Dictionary<int, Bookmark> _cachedSaveSlots;
        public Dictionary<int, BookmarkMetadata> SaveSlotsMetadata { get; private set; }

        /// <summary>
        /// Initalization of members which are unlikely to change in the future
        /// </summary>
        public void InitVariables()
        {
            _savePathBase = Application.persistentDataPath + "/Save/";
            _globalSavePath = _savePathBase + "global.nsav";
            _fileHeader = Encoding.ASCII.GetBytes("NOVASAVE");
            _cryptic = new DESCryptoServiceProvider
            {
                Key = Encoding.ASCII.GetBytes("NovaSave"),
                IV = Encoding.ASCII.GetBytes("novasave")
            };
            _formatter = new BinaryFormatter();
            _cachedSaveSlots = new Dictionary<int, Bookmark>();
            SaveSlotsMetadata = new Dictionary<int, BookmarkMetadata>();
        }

        public void Awake()
        {
            InitVariables();

            Directory.CreateDirectory(_savePathBase);
            if (File.Exists(_globalSavePath))
                using (var fs = File.OpenRead(_globalSavePath))
                    _globalSave = ReadSave<GlobalSave>(fs);
            else
                ResetGlobalSave();

            var regex = new Regex("sav([0-9]+).nsav");
            foreach (var name in Directory.GetFiles(_savePathBase, "sav*.nsav"))
            {
                var result = regex.Match(name);
                if (result.Groups.Count > 1)
                {
                    int id;
                    if (int.TryParse(result.Groups[1].Value, out id))
                        SaveSlotsMetadata.Add(id, new BookmarkMetadata
                        {
                            SaveId = id,
                            ModifiedTime = File.GetLastWriteTime(name)
                        });
                }
            }

            Debug.Log("CheckpointManager Initialized");
        }

        /// <summary>
        /// Set a dialogue to "reached" state and save the restore entry for the dialogue.
        /// </summary>
        /// <param name="nodeName">The name of FlowChartNode containing the dialogue.</param>
        /// <param name="dialogueIndex">The index of the dialogue.</param>
        /// <param name="entry">Restore entry for the dialogue</param>
        public void SetReached(string nodeName, int dialogueIndex, GameStateStepRestoreEntry entry)
        {
            _globalSave.SavedNodes.Ensure(nodeName).DialogueRestoreEntries[dialogueIndex] = entry;
        }

        /// <summary>
        /// Set a branch to "reached" state.
        /// </summary>
        /// <param name="nodeName">The name of FlowChartNode containing the branch.</param>
        /// <param name="branchName">The name of the branch.</param>
        public void SetReached(string nodeName, string branchName)
        {
            _globalSave.SavedNodes.Ensure(nodeName).ReachedBranches.Add(branchName);
        }

        /// <summary>
        /// Set an ending to "reached" state.
        /// </summary>
        /// <param name="endName">The name of the ending.</param>
        public void SetReached(string endName)
        {
            _globalSave.ReachedEndings.Add(endName);
        }

        /// <summary>
        /// Check if the dialogue has been reached and retrieve the restore entry.
        /// </summary>
        /// <param name="nodeName">The name of FlowChartNode containing the dialogue.</param>
        /// <param name="dialogueIndex">The index of the dialogue.</param>
        /// <returns>The restore entry for the dialogue. Null if not reached.</returns>
        public GameStateStepRestoreEntry IsReached(string nodeName, int dialogueIndex)
        {
            NodeSaveInfo info;
            GameStateStepRestoreEntry entry;
            if (_globalSave.SavedNodes.TryGetValue(nodeName, out info))
                if (info.DialogueRestoreEntries.TryGetValue(dialogueIndex, out entry))
                    return entry;
            return null;
        }

        /// <summary>
        /// Check if the branch has been reached.
        /// </summary>
        /// <param name="nodeName">The name of FlowChartNode containing the branch.</param>
        /// <param name="branchName">The name of the branch.</param>
        /// <returns>Whether the branch has been reached.</returns>
        public bool IsReached(string nodeName, string branchName)
        {
            NodeSaveInfo info;
            if (_globalSave.SavedNodes.TryGetValue(nodeName, out info))
                return info.ReachedBranches.Contains(branchName);
            return false;
        }

        /// <summary>
        /// Check if the ending has been reached.
        /// </summary>
        /// <param name="endName">The name of the ending.</param>
        /// <returns>Whether the ending has been reached.</returns>
        public bool IsReached(string endName)
        {
            return _globalSave.ReachedEndings.Contains(endName);
        }

        private string ComposeFileName(int saveId)
        {
            return string.Format("{0}sav{1:D3}.nsav", _savePathBase, saveId);
        }

        /// <summary>
        /// Reset the global save file to clear all progress.
        /// Note that all the other save files will be invalid.
        /// </summary>
        public void ResetGlobalSave()
        {
            using (var fs = File.OpenWrite(_globalSavePath))
                WriteSave(_globalSave = new GlobalSave(), fs);
        }

        private void WriteSave<T>(T obj, Stream s)
        {
            var bw = new BinaryWriter(s);
            bw.Write(_fileHeader);
            bw.Write(Version);
            using (var stream = new CryptoStream(s, _cryptic.CreateEncryptor(), CryptoStreamMode.Write))
                _formatter.Serialize(stream, obj);
        }

        private T ReadSave<T>(Stream s)
        {
            var bw = new BinaryReader(s);
            this.RuntimeAssert(_fileHeader.SequenceEqual(bw.ReadBytes(_fileHeader.Length)),
                "Invalid save file format");
            this.RuntimeAssert(Version >= bw.ReadInt32(),
                "Save file is incompatible with the current version of engine");
            using (var stream = new CryptoStream(s, _cryptic.CreateDecryptor(), CryptoStreamMode.Read))
                return (T) _formatter.Deserialize(stream);
        }

        /// <summary>
        /// Save a bookmark to disk, and update the global save file too.
        /// Will throw exception if it fails.
        /// </summary>
        /// <param name="saveId">File No. of the bookmark.</param>
        /// <param name="save">The bookmark to save.</param>
        public void SaveBookmark(int saveId, Bookmark save)
        {
            using (var fs = File.OpenWrite(ComposeFileName(saveId)))
            {
                save.GlobalSaveIdentifier = _globalSave.GlobalSaveIdentifier;
                WriteSave(_cachedSaveSlots[saveId] = save, fs);
            }

            using (var fs = File.OpenWrite(_globalSavePath))
                WriteSave(_globalSave, fs);

            SaveSlotsMetadata.Ensure(saveId).ModifiedTime = DateTime.Now;
        }

        /// <summary>
        /// Load a bookmark from disk. Never uses cache.
        /// Will throw exception if it fails.
        /// </summary>
        /// <param name="saveId">File No. of the bookmark.</param>
        /// <returns>The loaded bookmark.</returns>
        public Bookmark LoadBookmark(int saveId)
        {
            using (var fs = File.OpenRead(ComposeFileName(saveId)))
            {
                var result = ReadSave<Bookmark>(fs);
                this.RuntimeAssert(result.GlobalSaveIdentifier == _globalSave.GlobalSaveIdentifier,
                    "Save file is incompatible with the global save file");
                return _cachedSaveSlots[saveId] = result;
            }
        }

        /// <summary>
        /// Delete a specified bookmark.
        /// </summary>
        /// <param name="saveId">File No. of the bookmark.</param>
        public void DeleteBookmark(int saveId)
        {
            File.Delete(ComposeFileName(saveId));
            SaveSlotsMetadata.Remove(saveId);
            _cachedSaveSlots.Remove(saveId);
        }

        /// <summary>
        /// Load the contents of all existing bookmark in the given range eagerly.
        /// </summary>
        /// <param name="beginSaveId">The beginning of the range, inclusive.</param>
        /// <param name="endSaveId">The end of the range, exclusive.</param>
        public void EagerLoadRange(int beginSaveId, int endSaveId)
        {
            for (; beginSaveId < endSaveId; beginSaveId++)
            {
                if (SaveSlotsMetadata.ContainsKey(beginSaveId))
                    LoadBookmark(beginSaveId);
            }
        }

        /// <summary>
        /// Load / Save a bookmark by File No.. Will use cached result if exists.
        /// </summary>
        /// <param name="saveId">File No. of the bookmark.</param>
        /// <returns>The cached or loaded bookmark</returns>
        public Bookmark this[int saveId]
        {
            get
            {
                if (!SaveSlotsMetadata.ContainsKey(saveId))
                    return null;
                if (!_cachedSaveSlots.ContainsKey(saveId))
                    LoadBookmark(saveId);
                return _cachedSaveSlots[saveId];
            }

            set
            {
                SaveBookmark(saveId, value);
            }
        }

        /// <summary>
        /// Query the file No. of the latest / earliest bookmark.
        /// </summary>
        /// <param name="begin">Beginning file No. of the query range, inclusive.</param>
        /// <param name="end">Ending file No. of the query range, exclusive.</param>
        /// <param name="type">Type of this query.</param>
        /// <returns>File No. to query. If no bookmark is found in range, the return value will be "begin".</returns>
        public int QuerySaveIdByTime(int begin, int end, SaveIdQueryType type)
        {
            var filtered = SaveSlotsMetadata.Values.Where(m => m.SaveId >= begin && m.SaveId < end);
            if (!filtered.Any())
                return begin;
            if (type == SaveIdQueryType.Earliest)
                return filtered.Aggregate((agg, val) => agg.ModifiedTime < val.ModifiedTime ? agg : val).SaveId;
            else
                return filtered.Aggregate((agg, val) => agg.ModifiedTime > val.ModifiedTime ? agg : val).SaveId;
        }

        public int QueryMaxSaveId(int begin)
        {
            if (!SaveSlotsMetadata.Any())
            {
                return begin;
            }
            return Math.Max(SaveSlotsMetadata.Keys.Max(), begin);
        }

        public int QueryMinUnusedSaveId(int begin)
        {
            int saveId = begin;
            while (SaveSlotsMetadata.ContainsKey(saveId))
            {
                ++saveId;
            }
            return saveId;
        }
    }
}