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

        private static string GetAssetFullPath(UnityEngine.Object asset)
        {
            return Path.Combine(Path.GetDirectoryName(Application.dataPath), AssetDatabase.GetAssetPath(asset));
        }

        private const string ResourcesFolderName = "/Resources/";

        private static string GetResourcesFolder(string path)
        {
            path = Utils.ConvertPathSeparator(path);

            var index = path.IndexOf(ResourcesFolderName, StringComparison.Ordinal);
            if (index == -1)
            {
                throw new ArgumentException();
            }

            return path.Substring(0, index + ResourcesFolderName.Length);
        }

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
            var prefix = new string(
                fileNames.First()
                    .Substring(0, fileNames.Min(s => s.Length))
                    .TakeWhile((c, i) => fileNames.All(s => s[i] == c))
                    .ToArray()
            );

            prefix = prefix.TrimEnd('_');

            if (prefix.Length == 0)
            {
                prefix = fileNames.OrderBy(s => s).First();
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
                    displayNames = new List<LocaleStringPair>
                        {new LocaleStringPair {locale = I18n.DefaultLocale, value = fileName}},
                    resourcePath = GetResourcePath(imagePath)
                };
            }).ToList();

            EditorUtility.SetDirty(group);
        }

        [MenuItem("Assets/Create/Nova/Image Group", false)]
        public static void CreateImageGroup()
        {
            // split path name and file name
            var path = EditorUtils.GetSelectedDirectory();
            CreateImageGroup(path, EditorUtils.GetSelectedSpritePaths());
        }

        [MenuItem("Assets/Create/Nova/Image Group", true)]
        public static bool CreateImageGroupValidation()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(Texture2D);
        }

        private ReorderableList reorderableList;
        private SerializedProperty entries;

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
        }

        private bool previewDrawSnapshotFrame = true;
        private Color previewSnapshotFrameColor = Color.red;
        private float previewSnapshotFrameLineWidth = 1.0f;

        private static void CorrectSnapshotScaleY(ImageEntry entry, Texture tex, SerializedProperty entryProperty)
        {
            var size = entry.snapshotScale * new Vector2(tex.width, tex.height);
            var fix = Mathf.Abs(SnapshotAspectRatio / (size.y / size.x));
            entryProperty.FindPropertyRelative("snapshotScale.y").floatValue *= fix;
        }

        private static void CorrectSnapshotScaleX(ImageEntry entry, Texture tex, SerializedProperty entryProperty)
        {
            var size = entry.snapshotScale * new Vector2(tex.width, tex.height);
            var fix = Mathf.Abs((1.0f / SnapshotAspectRatio) / (size.x / size.y));
            entryProperty.FindPropertyRelative("snapshotScale.x").floatValue *= fix;
        }

        private static void ResetSnapshotScaleOffset(SerializedProperty entryProperty)
        {
            // use serialized property for proper save & undo
            entryProperty.FindPropertyRelative("snapshotOffset.x").floatValue = 0.0f;
            entryProperty.FindPropertyRelative("snapshotOffset.y").floatValue = 0.0f;
            entryProperty.FindPropertyRelative("snapshotScale.x").floatValue = 1.0f;
            entryProperty.FindPropertyRelative("snapshotScale.y").floatValue = 1.0f;
        }

        private void DrawPreview(string path, ImageEntry entry, SerializedProperty entryProperty)
        {
            var sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
            {
                EditorGUILayout.HelpBox("Invalid image resource path!", MessageType.Error);
                return;
            }

            if (GUILayout.Button("Correct Snapshot Scale Y for Aspect Ratio"))
            {
                CorrectSnapshotScaleY(entry, sprite.texture, entryProperty);
            }

            if (GUILayout.Button("Correct Snapshot Scale X for Aspect Ratio"))
            {
                CorrectSnapshotScaleX(entry, sprite.texture, entryProperty);
            }

            if (GUILayout.Button("Reset Snapshot Scale Offset"))
            {
                ResetSnapshotScaleOffset(entryProperty);
            }

            var height = EditorGUIUtility.currentViewWidth / sprite.texture.width * sprite.texture.height;
            var rect = EditorGUILayout.GetControlRect(false, height);
            EditorGUI.DrawPreviewTexture(rect, sprite.texture);
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
            var path = entry.FindPropertyRelative("resourcePath").stringValue;
            EditorGUILayout.Space();

            // preview snapshot frame options
            previewDrawSnapshotFrame = GUILayout.Toggle(previewDrawSnapshotFrame, "Preview Draw Snapshot Frame");
            previewSnapshotFrameColor = EditorGUILayout.ColorField("Snapshot Frame Color", previewSnapshotFrameColor);
            previewSnapshotFrameLineWidth = EditorGUILayout.Slider("Snapshot Frame Line Width",
                previewSnapshotFrameLineWidth, 0.5f, 4.0f);

            DrawPreview(path, Target.entries[index], entry);
        }

        private static RenderTexture _snapshotRenderTexture;

        public static RenderTexture SnapshotRenderTexture
        {
            get
            {
                if (_snapshotRenderTexture == null)
                {
                    _snapshotRenderTexture =
                        new RenderTexture(SnapshotWidth, SnapshotHeight, 0);
                }

                return _snapshotRenderTexture;
            }
        }

        private static Texture2D _snapshotTexture;

        public static Texture2D SnapshotTexture
        {
            get
            {
                if (_snapshotTexture == null)
                {
                    _snapshotTexture = new Texture2D(SnapshotWidth, SnapshotHeight);
                }

                return _snapshotTexture;
            }
        }

        private static byte[] GetSnapshotPNGData()
        {
            var oldRt = RenderTexture.active;
            RenderTexture.active = SnapshotRenderTexture;
            SnapshotTexture.ReadPixels(new Rect(0, 0, SnapshotWidth, SnapshotHeight), 0, 0);
            SnapshotTexture.Apply();
            RenderTexture.active = oldRt;
            return SnapshotTexture.EncodeToPNG();
        }

        private static void GenerateSnapshot(ImageEntry entry)
        {
            if (entry == null) return;
            var sprite = Resources.Load<Sprite>(entry.resourcePath);
            if (sprite == null) return;
            var tex = sprite.texture;
            Graphics.Blit(tex, SnapshotRenderTexture, entry.snapshotScale, entry.snapshotOffset);
            var data = GetSnapshotPNGData();

            var assetFullPath = GetAssetFullPath(sprite);
            var snapshotFullPath =
                Path.Combine(GetResourcesFolder(assetFullPath), entry.snapshotResourcePath + ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotFullPath));
            File.WriteAllBytes(snapshotFullPath, data);
        }

        private ImageGroup Target => target as ImageGroup;

        public static void GenerateSnapshot(ImageGroup group)
        {
            if (group == null || group.entries.Count <= 0) return;
            GenerateSnapshot(group.entries[0]);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (GUILayout.Button("Generate Snapshot"))
            {
                GenerateSnapshot(Target);
                AssetDatabase.Refresh();
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