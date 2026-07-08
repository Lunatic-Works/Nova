using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;

namespace Nova
{
    public class BookmarkManager : MonoBehaviour
    {
        private readonly Dictionary<int, Bookmark> cachedBookmarks = new Dictionary<int, Bookmark>();
        public readonly Dictionary<int, BookmarkMetadata> bookmarksMetadata = new Dictionary<int, BookmarkMetadata>();

        private CheckpointManager checkpointManager;
        private string savePathBase => checkpointManager.savePathBase;
        private GlobalSave globalSave => checkpointManager.globalSave;
        private CheckpointSerializer serializer => checkpointManager.serializer;

        private bool inited;

        public void Init()
        {
            if (inited)
            {
                return;
            }

            checkpointManager = Utils.FindNovaController().CheckpointManager;
            checkpointManager.Init();

            bookmarksMetadata.Clear();
            foreach (var fileName in Directory.GetFiles(savePathBase, "sav*.nsav"))
            {
                var result = Regex.Match(fileName, @"sav([0-9]+)\.nsav");
                if (result.Groups.Count > 1 && int.TryParse(result.Groups[1].Value, out var id))
                {
                    bookmarksMetadata.Add(id, new BookmarkMetadata
                    {
                        saveID = id,
                        // TODO: Avoid deserializing the bookmark just to get the creation time
                        // Maybe write the creation time in the filename
                        creationTime = serializer.ReadBookmark(fileName).creationTime
                    });
                }
            }

            inited = true;
        }

        private void Awake()
        {
            Init();
        }

        private void OnDestroy()
        {
            foreach (var bookmark in cachedBookmarks.Values)
            {
                bookmark.DestroyTexture();
            }
        }

        private string GetBookmarkFileName(int saveID)
        {
            return Path.Combine(savePathBase, $"sav{saveID:D3}.nsav");
        }

        private void ReplaceCache(int saveID, Bookmark bookmark)
        {
            if (cachedBookmarks.TryGetValue(saveID, out var old))
            {
                if (old == bookmark)
                {
                    return;
                }

                if (bookmark != null && bookmark.screenshot == null)
                {
                    bookmark.screenshot = old.screenshot;
                }
                else
                {
                    Destroy(old.screenshot);
                }
            }

            if (bookmark == null)
            {
                cachedBookmarks.Remove(saveID);
            }
            else
            {
                cachedBookmarks[saveID] = bookmark;
            }
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

            var fileName = GetBookmarkFileName(saveID);
            // Write with atomic move
            var tmpPath = fileName + ".tmp";
            serializer.WriteBookmark(tmpPath, bookmark);
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            File.Move(tmpPath, fileName);

            checkpointManager.UpdateGlobalSave();

            var metadata = bookmarksMetadata.Ensure(saveID);
            metadata.saveID = saveID;
            metadata.creationTime = bookmark.creationTime;

            ReplaceCache(saveID, bookmark);
        }

        public Bookmark LoadBookmark(int saveID, bool isUpgrading = false)
        {
            if (!bookmarksMetadata.ContainsKey(saveID))
            {
                return null;
            }

            if (cachedBookmarks.ContainsKey(saveID))
            {
                return cachedBookmarks[saveID];
            }

            var bookmark = serializer.ReadBookmark(GetBookmarkFileName(saveID));
            if (!isUpgrading && bookmark.globalSaveIdentifier != globalSave.identifier)
            {
                Debug.LogWarning(
                    $"Nova: Bookmark {saveID} " +
                    $"globalSaveIdentifier {bookmark.globalSaveIdentifier} != {globalSave.identifier}");
                bookmark = null;
            }

            ReplaceCache(saveID, bookmark);
            return bookmark;
        }

        public void DeleteBookmark(int saveID)
        {
            var fileName = GetBookmarkFileName(saveID);
            File.Delete(fileName);
            bookmarksMetadata.Remove(saveID);
            ReplaceCache(saveID, null);
        }

        /// <summary>
        /// Load the contents of all existing bookmark in the given range eagerly.
        /// </summary>
        /// <param name="begin">The beginning of the range, inclusive.</param>
        /// <param name="end">The end of the range, exclusive.</param>
        public void EagerLoadRange(int begin, int end)
        {
            for (var saveID = begin; saveID < end; ++saveID)
            {
                if (bookmarksMetadata.ContainsKey(saveID))
                {
                    LoadBookmark(saveID);
                }
            }
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
            {
                return begin;
            }

            if (type == SaveIDQueryType.Earliest)
            {
                return filtered.Aggregate((agg, val) => agg.creationTime < val.creationTime ? agg : val).saveID;
            }
            else // type == SaveIDQueryType.Latest
            {
                return filtered.Aggregate((agg, val) => agg.creationTime > val.creationTime ? agg : val).saveID;
            }
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
            var saveID = begin;
            while (saveID < end && bookmarksMetadata.ContainsKey(saveID))
            {
                ++saveID;
            }

            return saveID;
        }

        public string GetNodeName(Bookmark bookmark)
        {
            return checkpointManager.GetNodeRecord(bookmark.nodeOffset).name;
        }
    }
}
