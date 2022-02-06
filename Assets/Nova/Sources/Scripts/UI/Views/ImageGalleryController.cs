using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    using ImageUnlockInfo = SerializableHashSet<string>;

    public class ImageGalleryController : ViewControllerBase
    {
        public const string ImageUnlockStatusKey = "image_unlock_status";

        public ImageGroupList imageGroupList;
        public GameObject snapshotEntryPrefab;
        public int maxRow;
        public int maxCol;
        public Sprite emptyImage;
        public Sprite lockedImage;

        private CheckpointManager checkpointManager;

        private Button leftButton;
        private Button rightButton;
        private Text pageText;
        private ImageViewer imageViewer;

        private readonly List<ImageGalleryEntry> entries = new List<ImageGalleryEntry>();

        private int page = 1;

        private int pageCount => (imageGroupList.groups.Count + entries.Count - 1) / entries.Count;

        protected override void Awake()
        {
            base.Awake();

            checkpointManager = Utils.FindNovaGameController().CheckpointManager;

            var pagerPanel = myPanel.transform.Find("Snapshots/Footer/Pager");
            leftButton = pagerPanel.Find("LeftButton").GetComponent<Button>();
            rightButton = pagerPanel.Find("RightButton").GetComponent<Button>();
            pageText = pagerPanel.Find("PageText").GetComponent<Text>();
            imageViewer = myPanel.transform.Find("ImageViewer").GetComponent<ImageViewer>();

            leftButton.onClick.AddListener(PageLeft);
            rightButton.onClick.AddListener(PageRight);

            var entryGrid = myPanel.transform.Find("Snapshots/Content").GetComponent<GridLayoutGroup>();
            entryGrid.constraintCount = maxCol;
            for (int _ = 0; _ < maxRow * maxCol; ++_)
            {
                var entry = Instantiate(snapshotEntryPrefab, entryGrid.transform);
                entries.Add(entry.GetComponent<ImageGalleryEntry>());
            }
        }

        protected override void Start()
        {
            base.Start();

            checkpointManager.Init();

            imageViewer.Hide();
            ShowPage();
        }

        public override void Show(Action onFinish)
        {
            imageViewer.Hide();
            ShowPage();

            base.Show(onFinish);
        }

        private void ShowPage()
        {
            leftButton.interactable = page > 1;
            rightButton.interactable = page < pageCount;
            pageText.text = $"{page} / {pageCount}";

            var offset = (page - 1) * entries.Count;
            var groups = imageGroupList.groups;
            var unlockInfo = checkpointManager.Get(ImageUnlockStatusKey, new ImageUnlockInfo());
            for (var i = 0; i < entries.Count; ++i)
            {
                var entry = entries[i];
                entry.button.interactable = false;
                entry.button.onClick.RemoveAllListeners();
                entry.text.enabled = false;
                if (offset + i < groups.Count)
                {
                    var group = groups[offset + i];
                    if (group.entries.Count > 0)
                    {
                        int unlockedCount = GetUnlockedImageCount(group, unlockInfo);
                        if (unlockedCount > 0)
                        {
                            int firstUnlocked = GetNextUnlockedImage(group, unlockInfo, -1);
                            entry.snapshot.sprite =
                                Resources.Load<Sprite>(group.entries[firstUnlocked].snapshotResourcePath);
                            entry.button.interactable = true;
                            entry.button.onClick.AddListener(() => ShowGroup(group, unlockInfo));

                            if (group.entries.Count > 1)
                            {
                                entry.text.enabled = true;
                                entry.text.text = $"{unlockedCount} / {group.entries.Count}";
                            }
                        }
                        else
                        {
                            entry.snapshot.sprite = lockedImage;
                        }
                    }
                    else
                    {
                        entry.snapshot.sprite = emptyImage;
                    }
                }
                else
                {
                    entry.snapshot.sprite = emptyImage;
                }
            }
        }

        public static int GetUnlockedImageCount(ImageGroup group, ImageUnlockInfo unlockInfo)
        {
            return group.entries.Count(entry => unlockInfo.Contains(Utils.ConvertPathSeparator(entry.resourcePath)));
        }

        public static int GetPreviousUnlockedImage(ImageGroup group, ImageUnlockInfo unlockInfo, int start)
        {
            for (int i = start - 1; i >= 0; --i)
            {
                if (unlockInfo.Contains(Utils.ConvertPathSeparator(group.entries[i].resourcePath)))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int GetNextUnlockedImage(ImageGroup group, ImageUnlockInfo unlockInfo, int start)
        {
            for (int i = start + 1; i < group.entries.Count; ++i)
            {
                if (unlockInfo.Contains(Utils.ConvertPathSeparator(group.entries[i].resourcePath)))
                {
                    return i;
                }
            }

            return -1;
        }

        private void PageLeft()
        {
            if (page > 1)
            {
                --page;
            }

            ShowPage();
        }

        private void PageRight()
        {
            if (page < pageCount)
            {
                ++page;
            }

            ShowPage();
        }

        private void ShowGroup(ImageGroup group, ImageUnlockInfo unlockInfo)
        {
            imageViewer.Show(group, unlockInfo);
        }

        protected override void BackHide()
        {
            if (imageViewer.gameObject.activeSelf)
            {
                imageViewer.Hide();
            }
            else
            {
                Hide();
            }
        }

        #region For debug

        private void UnlockAllImages()
        {
            var unlockInfo = checkpointManager.Get(ImageUnlockStatusKey, new ImageUnlockInfo());
            foreach (var group in imageGroupList.groups)
            {
                foreach (var entry in group.entries)
                {
                    unlockInfo.Add(Utils.ConvertPathSeparator(entry.resourcePath));
                }
            }

            checkpointManager.Set(ImageUnlockStatusKey, unlockInfo);
            ShowPage();
        }

        protected override void OnActivatedUpdate()
        {
            base.OnActivatedUpdate();

            if (Utils.GetKeyDownInEditor(KeyCode.LeftShift))
            {
                UnlockAllImages();
            }
        }

        #endregion
    }
}