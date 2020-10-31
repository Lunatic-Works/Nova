using System;
using System.IO;
using UnityEngine;

namespace Nova
{
    [Serializable]
    public class BackgroundEntry
    {
        public string id;
        public LocaleStringPair[] displayNames;
        public string resourcePath;
        public Vector2 snapshotOffset = Vector2.zero;
        public Vector2 snapshotScale = Vector2.one;

        public string GetDisplayName()
        {
            foreach (var pair in displayNames)
            {
                if (pair.locale == I18n.CurrentLocale)
                {
                    return pair.value;
                }
            }

            return "(No title)";
        }

        public string snapshotResourcePath => Path.Combine(
            Path.GetDirectoryName(resourcePath),
            "Snapshots",
            Path.GetFileNameWithoutExtension(resourcePath) + ".__snapshot"
        );
    }
}