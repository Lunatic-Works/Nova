using System;
using UnityEngine;

namespace Nova
{
    [Serializable]
    public class Bookmark
    {
        public const int ScreenshotWidth = 320;
        public const int ScreenshotHeight = 180;

        public long nodeOffset;
        public long checkpointOffset;
        public int dialogueIndex;
        public DialogueDisplayData description;
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
                    Utils.RuntimeAssert(screenshotTexture == null, "Screenshot cache is not consistent.");
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

        public Bookmark(NodeRecord nodeRecord, long checkpointOffset, int dialogueIndex)
        {
            nodeOffset = nodeRecord.offset;
            this.checkpointOffset = checkpointOffset;
            this.dialogueIndex = dialogueIndex;
        }

        public void DestroyTexture()
        {
            Utils.DestroyObject(screenshotTexture);
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
}
