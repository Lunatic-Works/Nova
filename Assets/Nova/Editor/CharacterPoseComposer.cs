using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Nova.Editor
{
    public class CharacterPoseComposer : EditorWindow
    {
        private class Pose
        {
            public string name;
            public SpriteWithOffset sprite;
        }

        [MenuItem("Nova/Preview Character Pose")]
        [MenuItem("CONTEXT/CharacterController/Preview Character Pose")]
        public static void ShowWindow()
        {
            GetWindow(typeof(CharacterPoseComposer), false, "Character Pose Composer");
        }

        private string imageFolder;
        private int referenceSize = 2048;
        private int pixelsPerUnit = 100;

        private readonly List<Pose> poses = new List<Pose>();
        private ReorderableList reorderableList;
        private bool dirty;

        private GameObject root;
        private CharacterTextureMerger merger;
        private Texture texture;

        private RectInt captureBox = new RectInt(100, 100, 400, 400);
        private string captureDest;

        private void OnEnable()
        {
            imageFolder = EditorUtils.GetSelectedDirectory();

            root = new GameObject("PoseComposerDummy")
            {
                hideFlags = HideFlags.DontSave
            };
            merger = root.AddComponent<CharacterTextureMerger>();
            merger.runInEditMode = true;
            merger.referenceSize = referenceSize;
            merger.pixelsPerUnit = pixelsPerUnit;

            reorderableList = new ReorderableList(poses, typeof(Pose), true, true, true, true);
            reorderableList.drawHeaderCallback += DrawHeader;
            reorderableList.drawElementCallback += DrawElement;
            reorderableList.onAddCallback += AddItem;
            reorderableList.onRemoveCallback += RemoveItem;
            reorderableList.onReorderCallback += ReorderItem;
        }

        private void OnDisable()
        {
            reorderableList.drawHeaderCallback -= DrawHeader;
            reorderableList.drawElementCallback -= DrawElement;
            reorderableList.onAddCallback -= AddItem;
            reorderableList.onRemoveCallback -= RemoveItem;
            reorderableList.onReorderCallback -= ReorderItem;

            DestroyImmediate(root);
        }

        private static void DrawHeader(Rect rect)
        {
            GUI.Label(rect, "Pose Layers");
        }

        private void DrawElement(Rect rect, int index, bool active, bool focused)
        {
            Pose item = poses[index];

            if (EditorGUI.DropdownButton(
                new Rect(rect.x + rect.width / 3, rect.y, rect.width * 2 / 3, rect.height),
                new GUIContent(item.name ?? "Pose"),
                FocusType.Passive
            ))
            {
                var menu = new GenericMenu();

                var entries = AssetDatabase.FindAssets("t:Sprite", new[] {imageFolder})
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .ToList();

                foreach (var path in entries)
                {
                    var poseName = Path.GetFileNameWithoutExtension(path);
                    var sprite = AssetDatabase.LoadAssetAtPath<SpriteWithOffset>(Path.ChangeExtension(path, "asset"));
                    if (sprite == null)
                    {
                        return;
                    }

                    menu.AddItem(new GUIContent(poseName), false, () =>
                    {
                        item.name = poseName;
                        item.sprite = sprite;
                        dirty = true;
                    });
                }

                menu.ShowAsContext();
            }
        }

        private void AddItem(ReorderableList list)
        {
            poses.Add(new Pose());
            dirty = true;
        }

        private void RemoveItem(ReorderableList list)
        {
            poses.RemoveAt(list.index);
            dirty = true;
        }

        private void ReorderItem(ReorderableList list)
        {
            dirty = true;
        }

        private static string PosesToLuaTable(IEnumerable<Pose> poses)
        {
            var luaTable = poses
                .Where(p => !string.IsNullOrEmpty(p.name))
                .Select(p => "'" + p.name + "'")
                .Aggregate((r, s) => r + ", " + s);
            luaTable = "{" + luaTable + "}";
            return luaTable;
        }

        private void OnGUI()
        {
            GUILayout.Label("Character Pose Composer", EditorStyles.boldLabel);
            imageFolder = EditorGUILayout.TextField("Character Image Folder", imageFolder);
            referenceSize = EditorGUILayout.IntField("Reference Size", referenceSize);
            pixelsPerUnit = EditorGUILayout.IntField("Pixels Per Unit", pixelsPerUnit);

            reorderableList.DoLayoutList();

            if (dirty)
            {
                dirty = false;
                var sprites = poses.Where(p => !string.IsNullOrEmpty(p.name)).Select(p => p.sprite).ToList();
                texture = sprites.Count == 0 ? null : merger.GetMergedTexture(sprites);
            }

            if (texture == null)
            {
                return;
            }

            GUILayout.Label("Composed Pose Lua Table", EditorStyles.boldLabel);
            var luaTable = PosesToLuaTable(poses);
            EditorGUILayout.SelectableLabel(luaTable);
            if (GUILayout.Button("Copy"))
            {
                EditorGUIUtility.systemCopyBuffer = luaTable;
            }

            GUILayout.Label("Pose Preview", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Capture Box");
            captureBox = EditorGUILayout.RectIntField(captureBox);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            captureDest = EditorGUILayout.TextField("Capture Destination", captureDest);

            if (GUILayout.Button("Capture"))
            {
                Texture2D tex = new Texture2D(captureBox.width, captureBox.height, TextureFormat.ARGB32, false);
                RenderTexture.active = texture as RenderTexture;
                tex.ReadPixels(new Rect(captureBox.x, captureBox.y, captureBox.width, captureBox.height), 0, 0);
                RenderTexture.active = null;
                tex.Apply();

                var absoluteCaptureDest = Path.Combine(Path.GetDirectoryName(Application.dataPath), captureDest);
                Directory.CreateDirectory(Path.GetDirectoryName(absoluteCaptureDest));
                File.WriteAllBytes(absoluteCaptureDest, tex.EncodeToPNG());
                EditorUtility.DisplayDialog("Finished", $"The captured png is saved at {absoluteCaptureDest}", "OK");
            }

            GUILayout.EndHorizontal();

            var previewRect =
                EditorGUILayout.GetControlRect(false, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            previewRect.size = Utils.GetContentSize(previewRect.size, (float)texture.width / texture.height);
            var scale = previewRect.width / texture.width;
            EditorGUI.DrawPreviewTexture(previewRect, texture);
            EditorUtils.DrawPreviewCaptureFrame(previewRect, captureBox.ToRect(), scale, Color.red);
        }
    }
}