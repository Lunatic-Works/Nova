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
            var dir = EditorUtils.GetSelectedDirectory();
            var guids = AssetDatabase.FindAssets("t:MusicEntryList", new[] {dir});
            MusicEntryList list;
            if (guids.Length == 0)
            {
                list = CreateInstance<MusicEntryList>();
                AssetDatabase.CreateAsset(list, Path.Combine(dir, "MusicList.asset"));
            }
            else
            {
                list = AssetDatabase.LoadAssetAtPath<MusicEntryList>(AssetDatabase.GUIDToAssetPath(guids.First()));
            }

            list.entries = AssetDatabase.FindAssets("t:MusicEntry", new[] {dir})
                .Select(x => AssetDatabase.LoadAssetAtPath<MusicEntry>(AssetDatabase.GUIDToAssetPath(x)))
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
