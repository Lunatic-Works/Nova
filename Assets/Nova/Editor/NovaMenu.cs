using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nova.Editor
{
    public static class NovaMenu
    {
        [MenuItem("Nova/Clear Save Data", false, 1)]
        public static void ClearSaveData()
        {
            var saveDir = new DirectoryInfo(Application.persistentDataPath + "/Save/");
            foreach (var file in saveDir.GetFiles())
            {
                file.Delete();
            }
        }

        [MenuItem("Nova/Clear Config Data", false, 2)]
        public static void ClearConfigData()
        {
            PlayerPrefs.DeleteAll();
        }

        [MenuItem("Nova/Clear Input Mapping", false, 3)]
        public static void ClearInputMapping()
        {
            if (Directory.Exists(InputManager.InputFilesDirectory))
            {
                Directory.Delete(InputManager.InputFilesDirectory, true);
            }
        }

        [MenuItem("Nova/Clear All", false, 4)]
        public static void ClearAll()
        {
            ClearSaveData();
            ClearConfigData();
            ClearInputMapping();
        }
    }
}
