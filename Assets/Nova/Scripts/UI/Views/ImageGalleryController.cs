using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class ImageGalleryController : ViewControllerBase
    {
        public const string ImageUnlockStatusKey = "image_unlock_status";

        public ImageGroupList imageGroupList;
        public GameObject snapshotEntryPrefab;
        public int maxRow;
        public int maxCol;
        public Sprite emptyImage;
        public Sprite lockedImage;

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

            var pagerPanel = myPanel.transform.Find("Snapshots/Footer/Pager");
            leftButton = pagerPanel.Find("LeftButton").GetComponent<Button>();
            rightButton = pagerPanel.Find("RightButton").GetComponent<Button>();
            pageText = pagerPanel.Find("PageText").GetComponent<Text>();
            imageViewer = GetComponentInChildren<ImageViewer>(true);

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
            for (var i = 0; i < entries.Count; ++i)
            {
                if (offset + i < groups.Count)
                {
                    entries[i].SetGroup(groups[offset + i]);
                }
                else
                {
                    entries[i].SetGroup(emptyImage);
                }
            }
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

        public void DisplayGroup(ImageGroup group)
        {
            imageViewer.SetImageGroup(group);
            imageViewer.Show();
        }
    }
}