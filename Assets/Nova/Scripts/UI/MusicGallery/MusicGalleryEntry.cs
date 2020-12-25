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
        private Action<MusicListEntry> onClick;

        private void Awake()
        {
            text = GetComponentInChildren<Text>();
            button = GetComponent<Button>();
        }

        private void ForceUpdate()
        {
            if (entry == null) return;
            if (text == null) return;
            text.text = entry.entry.GetDisplayName();
            if (button == null) return;
            button.onClick.AddListener(() => onClick(entry));
        }

        private void OnEnable()
        {
            ForceUpdate();
        }

        public void Init(MusicListEntry entry, Action<MusicListEntry> onClick)
        {
            this.entry = entry;
            this.onClick = onClick;
            ForceUpdate();
        }
    }
}