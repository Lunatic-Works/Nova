using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    public class AbstractKeyList : MonoBehaviour
    {
        public Transform content;
        public AbstractKeyEntry entryPrefab;
        public InputMappingController controller;

        private readonly List<AbstractKeyEntry> entries = new List<AbstractKeyEntry>();

        private void ClearContent()
        {
            var children = content.Cast<Transform>().ToList();
            foreach (var child in children)
            {
                Destroy(child.gameObject);
            }

            entries.Clear();
        }

        public void RefreshAll()
        {
            ClearContent();
            foreach (var key in controller.mappableKeys)
            {
                var entry = Instantiate(entryPrefab, content);
                entry.Init(controller, key);
                entries.Add(entry);
            }
        }

        public void RefreshSelection()
        {
            foreach (var entry in entries)
            {
                entry.RefreshColor();
            }
        }
    }
}