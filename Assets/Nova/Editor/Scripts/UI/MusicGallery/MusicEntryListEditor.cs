using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nova.Editor
{
    [CustomEditor(typeof(MusicEntryList))]
    public class MusicEntryListEditor : SimpleEntryListEditor
    {
        [MenuItem("Assets/Nova/Create List for All Music Entries", false)]
        public static void CreateListForAllMusicEntries()
        {
            var path = EditorUtils.GetSelectedDirectory();
            var listPaths = AssetDatabase.FindAssets("t:MusicEntryList", new[] {path});
            MusicEntryList list;
            if (listPaths.Length == 0)
            {
                list = CreateInstance<MusicEntryList>();
                var pathName = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(list, Path.Combine(path, pathName + "_music_list.asset"));
            }
            else
            {
                list = AssetDatabase.LoadAssetAtPath<MusicEntryList>(AssetDatabase.GUIDToAssetPath(listPaths.First()));
            }

            list.entries = AssetDatabase.FindAssets("t:MusicEntry", new[] {path})
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<MusicEntry>)
                .ToList();

            EditorUtility.SetDirty(list);
        }

        protected override SerializedProperty GetEntriesProperty()
        {
            return serializedObject.FindProperty("entries");
        }

        protected override GUIContent GetEntryLabelContent(int i)
        {
            return new GUIContent($"Music {i:D2}");
        }

        protected override GUIContent GetHeaderContent()
        {
            return new GUIContent("Music Entries");
        }
    }
}