using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Nova.Editor
{
    [CustomEditor(typeof(ImageGroup))]
    public class ImageGroupEditor : UnityEditor.Editor
    {
        public const int SnapshotWidth = 320;
        public const int SnapshotHeight = 180;
        public const float SnapshotAspectRatio = (float)SnapshotHeight / SnapshotWidth;
        public const string ResourcesFolderName = "/Resources/";

        private static string GetResourcePath(string path)
        {
            path = Utils.ConvertPathSeparator(path);

            var index = path.IndexOf(ResourcesFolderName, StringComparison.Ordinal);
            if (index == -1)
            {
                throw new ArgumentException();
            }

            var resourcePath = path.Substring(index + ResourcesFolderName.Length);
            var dirName = Path.GetDirectoryName(resourcePath);
            var fileName = Path.GetFileNameWithoutExtension(resourcePath);
            resourcePath = Path.Combine(dirName, fileName);
            resourcePath = Utils.ConvertPathSeparator(resourcePath);
            return resourcePath;
        }

        private static string GetCommonPrefix(IEnumerable<string> paths)
        {
            var fileNames = paths.Select(Path.GetFileNameWithoutExtension).ToList();
            var prefix = string.Join("",
                fileNames.First()
                    .Substring(0, fileNames.Min(s => s.Length))
                    .TakeWhile((c, i) => fileNames.All(s => s[i] == c))
            );

            prefix = prefix.TrimEnd('_');

            if (prefix.Length == 0)
            {
                prefix = fileNames.Min();
            }

            return prefix;
        }

        private static void CreateImageGroup(string path, IEnumerable<string> imagePaths)
        {
            var imagePathList = imagePaths.ToList();
            var groupPath = Path.Combine(path, GetCommonPrefix(imagePathList) + "_group.asset");
            var group = AssetDatabase.LoadAssetAtPath<ImageGroup>(groupPath);
            if (group == null)
            {
                group = CreateInstance<ImageGroup>();
                AssetDatabase.CreateAsset(group, groupPath);
            }

            group.entries = imagePathList.Select(imagePath =>
            {
                var fileName = Path.GetFileNameWithoutExtension(imagePath);
                return new ImageEntry
                {
                    id = fileName,
                    displayNames = new SerializableDictionary<SystemLanguage, string> {[I18n.DefaultLocale] = fileName},
                    resourcePath = GetResourcePath(imagePath)
                };
            }).ToList();

            EditorUtility.SetDirty(group);
        }

        [MenuItem("Assets/Create/Nova/Image Group", false)]
        public static void CreateImageGroup()
        {
            // split path name and file name
            var dir = EditorUtils.GetSelectedDirectory();
            CreateImageGroup(dir, EditorUtils.GetSelectedSpritePaths());
        }

        [MenuItem("Assets/Create/Nova/Image Group", true)]
        public static bool CreateImageGroupValidation()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(Texture2D);
        }

        private ImageGroupCapturer capturer;
        private ReorderableList reorderableList;
        private SerializedProperty entries;
        private string previewEntryKey;

        private void OnEnable()
        {
            entries = serializedObject.FindProperty("entries");
            reorderableList = new ReorderableList(serializedObject, entries,
                true, true, true, true);

            reorderableList.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, new GUIContent("Images")); };

            reorderableList.onAddCallback = list =>
            {
                var index = list.index == -1 ? entries.arraySize : list.index;
                entries.InsertArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();
            };

            reorderableList.onRemoveCallback = list =>
            {
                if (list.index == -1) return;
                entries.DeleteArrayElementAtIndex(list.index);
                serializedObject.ApplyModifiedProperties();
            };

            capturer = new ImageGroupCapturer();
            previewEntryKey = "";
        }

        private void OnDisable()
        {
            capturer.OnDestroy();
            capturer = null;
        }

        private bool previewDrawSnapshotFrame = true;
        private Color previewSnapshotFrameColor = Color.red;
        private float previewSnapshotFrameLineWidth = 1.0f;

        private static void CorrectSnapshotScaleY(Vector2 size, SerializedProperty entryProperty)
        {
            size.Scale(entryProperty.FindPropertyRelative("snapshotScale").vector2Value);
            var fix = Mathf.Abs(SnapshotAspectRatio / (size.y / size.x));
            entryProperty.FindPropertyRelative("snapshotScale.y").floatValue *= fix;
        }

        private static void CorrectSnapshotScaleX(Vector2 size, SerializedProperty entryProperty)
        {
            size.Scale(entryProperty.FindPropertyRelative("snapshotScale").vector2Value);
            var fix = Mathf.Abs((1.0f / SnapshotAspectRatio) / (size.x / size.y));
            entryProperty.FindPropertyRelative("snapshotScale.x").floatValue *= fix;
        }

        private static void ResetSnapshotScaleOffset(SerializedProperty entryProperty)
        {
            // use serialized property for proper save & undo
            entryProperty.FindPropertyRelative("snapshotOffset").vector2Value = Vector2.zero;
            entryProperty.FindPropertyRelative("snapshotScale").vector2Value = Vector2.one;
        }

        private void DrawPreview(ImageEntry entry, SerializedProperty entryProperty)
        {
            Texture previewTexture = null;
            if (entry.composite)
            {
                if (capturer.renderTexture == null || previewEntryKey != entry.unlockKey)
                {
                    if (!capturer.DrawComposite(entry))
                    {
                        EditorGUILayout.HelpBox("Invalid image resource path or pose string!", MessageType.Error);
                        return;
                    }

                    previewEntryKey = entry.unlockKey;
                }

                previewTexture = capturer.renderTexture;
            }
            else
            {
                var sprite = Resources.Load<Sprite>(entry.resourcePath);
                if (sprite == null)
                {
                    EditorGUILayout.HelpBox("Invalid image resource path!", MessageType.Error);
                    return;
                }

                previewTexture = sprite.texture;
            }

            var size = new Vector2(previewTexture.width, previewTexture.height);

            if (GUILayout.Button("Correct Snapshot Scale Y for Aspect Ratio"))
            {
                CorrectSnapshotScaleY(size, entryProperty);
            }

            if (GUILayout.Button("Correct Snapshot Scale X for Aspect Ratio"))
            {
                CorrectSnapshotScaleX(size, entryProperty);
            }

            if (GUILayout.Button("Reset Snapshot Scale Offset"))
            {
                ResetSnapshotScaleOffset(entryProperty);
            }

            var height = EditorGUIUtility.currentViewWidth / previewTexture.width * previewTexture.height;
            var rect = EditorGUILayout.GetControlRect(false, height);
            EditorGUI.DrawPreviewTexture(rect, previewTexture);
            if (previewDrawSnapshotFrame)
            {
                DrawPreviewSnapshotFrame(entry, rect);
            }
        }

        private void DrawPreviewSnapshotFrame(ImageEntry entry, Rect rect)
        {
            EditorUtils.DrawPreviewCropFrame(rect, new Rect(entry.snapshotOffset, entry.snapshotScale),
                previewSnapshotFrameColor, previewSnapshotFrameLineWidth);
        }

        private void DrawEntry(int index)
        {
            var entry = entries.GetArrayElementAtIndex(index);
            EditorGUILayout.PropertyField(entry, true);
            EditorGUILayout.Space();

            // preview snapshot frame options
            previewDrawSnapshotFrame = GUILayout.Toggle(previewDrawSnapshotFrame, "Preview Draw Snapshot Frame");
            previewSnapshotFrameColor = EditorGUILayout.ColorField("Snapshot Frame Color", previewSnapshotFrameColor);
            previewSnapshotFrameLineWidth = EditorGUILayout.Slider("Snapshot Frame Line Width",
                previewSnapshotFrameLineWidth, 0.5f, 4.0f);

            DrawPreview(Target.entries[index], entry);
        }

        private ImageGroup Target => target as ImageGroup;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (GUILayout.Button("Generate Snapshot"))
            {
                capturer.GenerateSnapshot(Target);
                AssetDatabase.Refresh();
                previewEntryKey = "";
            }

            EditorGUILayout.HelpBox("The first image entry will be selected as the snapshot", MessageType.Info);

            reorderableList.DoLayoutList();
            if (reorderableList.index == -1 || reorderableList.index >= Target.entries.Count)
            {
                EditorGUILayout.LabelField(new GUIContent("Nothing Selected"));
            }
            else
            {
                DrawEntry(reorderableList.index);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
