using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Nova.Editor
{
    [CustomEditor(typeof(ImageGroupList))]
    public class ImageGroupListEditor : SimpleEntryListEditor
    {
        private ImageGroupList Target => target as ImageGroupList;

        protected override SerializedProperty GetEntriesProperty()
        {
            return serializedObject.FindProperty("groups");
        }

        protected override GUIContent GetEntryLabelContent(int i)
        {
            return new GUIContent($"Group {i:D2}");
        }

        protected override GUIContent GetHeaderContent()
        {
            return new GUIContent("Image Groups");
        }

        [MenuItem("Assets/Nova/Create List for All Image Groups", false)]
        public static void CreateListForAllImageGroups()
        {
            var dir = EditorUtils.GetSelectedDirectory();
            var guids = AssetDatabase.FindAssets("t:ImageGroupList", new[] {dir});
            ImageGroupList list;
            if (guids.Length == 0)
            {
                list = CreateInstance<ImageGroupList>();
                AssetDatabase.CreateAsset(list, Path.Combine(dir, "ImageGroupList.asset"));
            }
            else
            {
                list = AssetDatabase.LoadAssetAtPath<ImageGroupList>(AssetDatabase.GUIDToAssetPath(guids.First()));
            }

            list.groups = AssetDatabase.FindAssets("t:ImageGroup", new[] {dir})
                .Select(x => AssetDatabase.LoadAssetAtPath<ImageGroup>(AssetDatabase.GUIDToAssetPath(x)))
                .ToList();

            EditorUtility.SetDirty(list);
        }

        private class ValidationResultList
        {
            public readonly List<int> indices = new List<int>();
            private ReorderableList invalidGroupsList;

            public void Init(SerializedProperty entries, string name)
            {
                invalidGroupsList =
                    new ReorderableList(indices, typeof(ImageGroup), false, false, false, false);

                invalidGroupsList.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, new GUIContent(name)); };

                invalidGroupsList.drawElementCallback = (rect, index, active, focused) =>
                {
                    rect.height -= 2;
                    var center = rect.center;
                    center.y += 1;
                    rect.center = center;
                    EditorGUI.PropertyField(rect,
                        entries.GetArrayElementAtIndex(indices[index]));
                };

                invalidGroupsList.elementHeight = 20;
            }

            public void TryDoLayoutList()
            {
                if (!empty)
                {
                    invalidGroupsList.DoLayoutList();
                }
            }

            public bool empty => indices.Count == 0;
        }

        private readonly ValidationResultList badReferenceGroupList = new ValidationResultList();
        private readonly ValidationResultList badSnapshotAspectRatioGroupList = new ValidationResultList();
        private readonly ValidationResultList emptyGroupList = new ValidationResultList();

        private bool validated;

        private ImageGroupCapturer capturer;

        protected override void Init()
        {
            validated = false;
            badReferenceGroupList.Init(GetEntriesProperty(), "Has Bad Resources Reference");
            badSnapshotAspectRatioGroupList.Init(GetEntriesProperty(), "Bad Snapshot Aspect Ratio");
            emptyGroupList.Init(GetEntriesProperty(), "Empty Groups");
            capturer = new ImageGroupCapturer();
        }

        private void OnDisable()
        {
            capturer.OnDestroy();
            capturer = null;
        }

        private static bool GroupResourcesReferenceIsCorrect(ImageGroup group)
        {
            foreach (var entry in group.entries)
            {
                if (entry.composite)
                {
                    var sprites = CompositeSpriteController.LoadSprites(entry.resourcePath, entry.poseString);
                    if (!sprites.Any() || sprites.Contains(null))
                    {
                        return false;
                    }
                }
                else
                {
                    var path = entry.resourcePath;
                    if (Resources.Load<Sprite>(path) == null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool GroupSnapshotAspectRatioIsCorrect(ImageGroup group)
        {
            if (group.entries.Count == 0) return true;
            var entry = group.entries[0];
            var path = entry.resourcePath;
            var sprite = Resources.Load<Sprite>(path);
            if (sprite == null) return true;
            var tex = sprite.texture;
            var size = entry.snapshotScale * new Vector2(tex.width, tex.height);
            return Math.Abs(ImageGroupEditor.SnapshotAspectRatio - size.y / size.x) < 3 * float.Epsilon;
        }

        private static bool GroupIsEmpty(ImageGroup group)
        {
            return group.entries.Count == 0;
        }

        private void ValidateGroups()
        {
            validated = true;
            badReferenceGroupList.indices.Clear();
            badSnapshotAspectRatioGroupList.indices.Clear();
            emptyGroupList.indices.Clear();
            var groups = Target.groups;
            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                if (group == null) continue;

                if (GroupIsEmpty(group))
                {
                    emptyGroupList.indices.Add(i);
                }

                if (!GroupResourcesReferenceIsCorrect(group))
                {
                    badReferenceGroupList.indices.Add(i);
                }

                if (!GroupSnapshotAspectRatioIsCorrect(group))
                {
                    badSnapshotAspectRatioGroupList.indices.Add(i);
                }
            }
        }

        private bool noValidationError => badReferenceGroupList.empty && badSnapshotAspectRatioGroupList.empty;

        private void DrawValidationResult()
        {
            badReferenceGroupList.TryDoLayoutList();
            badSnapshotAspectRatioGroupList.TryDoLayoutList();
            emptyGroupList.TryDoLayoutList();
        }

        private void GenerateSnapshotForAllGroups()
        {
            foreach (var group in Target.groups)
            {
                capturer.GenerateSnapshot(group);
            }

            AssetDatabase.Refresh();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button(new GUIContent("Validate Groups")))
            {
                ValidateGroups();
            }

            if (!validated)
            {
                // draw nothing
            }
            else if (noValidationError)
            {
                EditorGUILayout.HelpBox("Good, no invalid group found.", MessageType.Info);
            }
            else
            {
                DrawValidationResult();
            }

            if (GUILayout.Button("Generate Snapshot for All Groups"))
            {
                GenerateSnapshotForAllGroups();
            }
        }
    }
}
