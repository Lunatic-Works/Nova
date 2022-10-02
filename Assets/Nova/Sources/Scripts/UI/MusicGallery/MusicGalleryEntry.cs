using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class MusicGalleryEntry : MonoBehaviour
    {
        private Text text;
        private Button button;
        private MusicListEntry entry;
        private bool inited;

        private void InitReferences()
        {
            if (inited)
            {
                return;
            }

            text = GetComponentInChildren<Text>();
            button = GetComponent<Button>();

            inited = true;
        }

        private void UpdateText()
        {
            if (!inited || entry == null)
            {
                return;
            }

            text.text = entry.entry.GetDisplayName();
        }

        public void Init(MusicListEntry entry, Action<MusicListEntry> onClick)
        {
            InitReferences();
            this.entry = entry;
            UpdateText();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick(entry));
        }

        private void OnEnable()
        {
            UpdateText();
            I18n.LocaleChanged.AddListener(UpdateText);
        }

        private void OnDisable()
        {
            I18n.LocaleChanged.RemoveListener(UpdateText);
        }
    }
}
