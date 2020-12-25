using System;
using UnityEngine.UI;

namespace Nova
{
    public class ImageGalleryController : ViewControllerBase
    {
        public ImageGroupList imageGroupList;
        public Text pageNumberLabel;
        public ImageViewer imageViewer;

        private ImageGalleryElement[] elements;

        protected override void Awake()
        {
            base.Awake();
            elements = GetComponentsInChildren<ImageGalleryElement>(true);
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

        private int pageCount => (imageGroupList.groups.Count + elements.Length - 1) / elements.Length;

        private void Refresh()
        {
            var offset = page * elements.Length;
            var groups = imageGroupList.groups;
            for (var i = 0; i < elements.Length; i++)
            {
                elements[i].SetGroup(offset + i < groups.Count ? groups[offset + i] : null);
            }

            pageNumberLabel.text = $"{page + 1}/{pageCount}";
        }

        public void NextPage()
        {
            page++;
            if (page * elements.Length >= imageGroupList.groups.Count)
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

        public void DisplayGroup(ImageGroup group)
        {
            imageViewer.SetImageGroup(group);
            imageViewer.Show();
        }
    }
}