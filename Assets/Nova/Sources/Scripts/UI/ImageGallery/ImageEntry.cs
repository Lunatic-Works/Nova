using System;
using System.IO;
using UnityEngine;

namespace Nova
{
    [Serializable]
    public class ImageEntry
    {
        public string id;
        public SerializableDictionary<SystemLanguage, string> displayNames;
        public bool composite;
        public string poseString;
        public string resourcePath;
        public Vector2 snapshotOffset = Vector2.zero;
        public Vector2 snapshotScale = Vector2.one;

        public string GetDisplayName()
        {
            if (displayNames.ContainsKey(I18n.CurrentLocale))
            {
                return displayNames[I18n.CurrentLocale];
            }
            else
            {
                return "(No title)";
            }
        }

        public string snapshotResourcePath => Path.Combine(
            composite ? resourcePath : Path.GetDirectoryName(resourcePath),
            "Snapshots",
            (composite ? id : Path.GetFileNameWithoutExtension(resourcePath)) + ".__snapshot"
        );

        public string unlockKey =>
            Utils.ConvertPathSeparator(composite ? Path.Combine(resourcePath, poseString) : resourcePath);
    }
}
