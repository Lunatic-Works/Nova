using System;
using UnityEngine.UI;

namespace Nova
{
    public class BackgroundGalleryController : ViewControllerBase
    {
        public BackgroundGroupList backgroundGroupList;
        public Text pageNumberLabel;
        public BackgroundImageViewer imageViewer;

        private BackgroundGalleryElement[] elements;

        protected override void Awake()
        {
            base.Awake();
            elements = GetComponentsInChildren<BackgroundGalleryElement>(true);
        }

        private int page = 0;

        private void OnEnable()
        {
            Refresh();
        }

        public override void Show(Action onFinish)
        {
            imageViewer.Hide();
            base.Show(onFinish);
        }

        private int pageCount => (backgroundGroupList.groups.Count + elements.Length - 1) / elements.Length;

        private void Refresh()
        {
            var offset = page * elements.Length;
            var groups = backgroundGroupList.groups;
            for (var i = 0; i < elements.Length; i++)
            {
                elements[i].SetGroup(offset + i < groups.Count ? groups[offset + i] : null);
            }

            pageNumberLabel.text = $"{page + 1}/{pageCount}";
        }

        public void NextPage()
        {
            page++;
            if (page * elements.Length >= backgroundGroupList.groups.Count)
            {
                page--;
            }

            Refresh();
        }

        public void PreviousPage()
        {
            page--;
            if (page < 0)
            {
                page = 0;
            }

            Refresh();
        }

        public void DisplayGroup(BackgroundGroup group)
        {
            imageViewer.SetBackgroundGroup(group);
            imageViewer.Show();
        }
    }
}