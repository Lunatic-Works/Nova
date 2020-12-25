using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    public class ImageGroup : ScriptableObject
    {
        public List<ImageEntry> entries;

        public Sprite LoadSnapshot()
        {
            if (entries.Count == 0) return null;
            return Resources.Load<Sprite>(entries[0].snapshotResourcePath);
        }
    }
}