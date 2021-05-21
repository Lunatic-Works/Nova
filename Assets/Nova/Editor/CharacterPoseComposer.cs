using System;
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
            public string poseName;
            public string posePath;
            public SpriteWithOffset sprite => AssetDatabase.LoadAssetAtPath<SpriteWithOffset>(posePath);
        }

        [MenuItem("Nova/Preview Character Pose")]
        [MenuItem("CONTEXT/CharacterController/Preview Character Pose")]
        public static void ShowWindow()
        {
            GetWindow(typeof(CharacterPoseComposer), false, "Character Pose Composer");
        }

        private string imageFolder;
        public int referenceSize = 2048;
        public int pixelsPerUnit = 100;

        private ReorderableList reorderableList;
        private readonly List<Pose> poses = new List<Pose>();
        private GameObject root;
        private CharacterTextureMerger merger;
        private Texture current;
        private float previewHeight = 500f;
        private bool dirty;
        private RectInt captureBox = new RectInt(100, 100, 500, 500);
        private string captureDest = "Faces/001.png";

        private void OnSelectionChange()
        {
            var go = Selection.activeGameObject;
            if (go != null)
            {
                var cc = go.GetComponent<CompositeSpriteControllerBase>();
                if (cc != null)
                {
                    imageFolder = cc.imageFolder;
                }
            }
        }

        private void OnEnable()
        {
            root = new GameObject("PoseComposerDummy")
            {
                hideFlags = HideFlags.DontSave
            };
            root.SetActive(true);
            merger = root.AddComponent<CharacterTextureMerger>();
            merger.runInEditMode = true;
            merger.referenceSize = referenceSize;
            merger.pixelsPerUnit = pixelsPerUnit;

            reorderableList = new ReorderableList(poses, typeof(Pose), true, true, true, true);

            reorderableList.drawHeaderCallback += DrawHeader;
            reorderableList.drawElementCallback += DrawElement;

            reorderableList.onReorderCallback += ReorderItem;

            reorderableList.onAddCallback += AddItem;
            reorderableList.onRemoveCallback += RemoveItem;

            OnSelectionChange();
        }

        private void OnDisable()
        {
            reorderableList.drawHeaderCallback -= DrawHeader;
            reorderableList.drawElementCallback -= DrawElement;

            reorderableList.onReorderCallback -= ReorderItem;

            reorderableList.onAddCallback -= AddItem;
            reorderableList.onRemoveCallback -= RemoveItem;

            DestroyImmediate(root);
        }

        private static void DrawHeader(Rect rect)
        {
            GUI.Label(rect, "Pose Layers");
        }

        private static IEnumerable<string> SubDirectories(string baseDirectory)
        {
            var subDirs = Directory.GetDirectories(baseDirectory);
            if (subDirs.Length == 0)
            {
                yield return baseDirectory;
            }
            else
            {
                foreach (var path in subDirs)
                {
                    foreach (var subPath in SubDirectories(path))
                    {
                        yield return subPath;
                    }
                }
            }
        }

        private void DrawElement(Rect rect, int index, bool active, bool focused)
        {
            Pose item = poses[index];

            if (EditorGUI.DropdownButton(
                new Rect(rect.x + rect.width / 3, rect.y, rect.width * 2 / 3, rect.height),
                new GUIContent(item.poseName ?? "Pose"),
                FocusType.Passive
            ))
            {
                var menu = new GenericMenu();

                string basePath = Path.Combine("Assets/Resources", imageFolder);
                var entries = AssetDatabase.FindAssets("t:Sprite", new[] {basePath})
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .ToList();

                foreach (var path in entries)
                {
                    var pose = Path.GetFileName(path);
                    menu.AddItem(new GUIContent(pose), false, () =>
                    {
                        item.poseName = pose;
                        item.posePath = path;
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

        private void ReorderItem(ReorderableList list)
        {
            dirty = true;
        }

        private void RemoveItem(ReorderableList list)
        {
            poses.RemoveAt(list.index);
            dirty = true;
        }

        private string PosePathToLuaAssetPath(string posePath)
        {
            var relative = posePath.Substring(posePath.LastIndexOf(imageFolder, StringComparison.Ordinal) +
                                              imageFolder.Length + 1);
            var extensionStripped = Path.ChangeExtension(relative, "");
            return extensionStripped.Substring(0, extensionStripped.Length - 1);
        }

        private void OnGUI()
        {
            GUILayout.Label("Character Pose Composer", EditorStyles.boldLabel);
            imageFolder = EditorGUILayout.TextField("Character Image Folder", imageFolder);
            referenceSize = EditorGUILayout.IntField("Reference Size", referenceSize);
            pixelsPerUnit = EditorGUILayout.IntField("Pixels Per Unit", pixelsPerUnit);

            if (!string.IsNullOrEmpty(imageFolder))
            {
                reorderableList.DoLayoutList();
                GUILayout.Label("Composed Pose Lua", EditorStyles.boldLabel);
                var luaList = poses
                    .Where(p => p.posePath != null && p.posePath.Contains(imageFolder))
                    .Select(p => "'" + PosePathToLuaAssetPath(p.posePath) + "'")
                    .DefaultIfEmpty()
                    .Aggregate((r, s) => r + ", " + s);
                luaList = "{" + luaList + "}";
                EditorGUILayout.SelectableLabel(luaList);
                if (GUILayout.Button("Copy"))
                {
                    EditorGUIUtility.systemCopyBuffer = luaList;
                }

                GUILayout.Label("Pose Preview", EditorStyles.boldLabel);
                previewHeight = EditorGUILayout.Slider("Preview Size", previewHeight, 200f, 1000f);
                if (dirty)
                {
                    dirty = false;
                    current = merger.GetMergedTexture(poses.Where(p => p.sprite != null).Select(p => p.sprite)
                        .ToList());
                }

                if (current != null)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Capture Box");
                    captureBox = EditorGUILayout.RectIntField(captureBox);
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    captureDest = EditorGUILayout.TextField("Capture Destination", captureDest);
                    if (GUILayout.Button("Capture"))
                    {
                        Texture2D tex = new Texture2D(captureBox.width, captureBox.height, TextureFormat.ARGB32, false);
                        RenderTexture.active = current as RenderTexture;
                        tex.ReadPixels(new Rect(captureBox.x, captureBox.y, captureBox.width, captureBox.height), 0, 0);
                        RenderTexture.active = null;
                        tex.Apply();

                        var dest = Path.Combine("Assets/Resources", captureDest);
                        File.WriteAllBytes(dest, tex.EncodeToPNG());
                        EditorUtility.DisplayDialog("Finished", "The captured png is saved at " + dest, "Good");
                    }

                    GUILayout.EndHorizontal();
                }

                if (current != null)
                {
                    var scale = previewHeight / current.height;
                    var previewRect = EditorGUILayout.GetControlRect(
                        false, previewHeight, GUILayout.Width(scale * current.width)
                    );
                    EditorGUI.DrawPreviewTexture(previewRect, current);

                    EditorGUI.DrawRect(
                        new Rect(
                            previewRect.x + captureBox.x * scale,
                            previewRect.y + captureBox.y * scale,
                            1,
                            captureBox.height * scale
                        ),
                        Color.red
                    );

                    EditorGUI.DrawRect(
                        new Rect(
                            previewRect.x + captureBox.xMax * scale,
                            previewRect.y + captureBox.y * scale,
                            1,
                            captureBox.height * scale
                        ),
                        Color.red
                    );

                    EditorGUI.DrawRect(
                        new Rect(
                            previewRect.x + captureBox.x * scale,
                            previewRect.y + captureBox.y * scale,
                            captureBox.width * scale,
                            1
                        ),
                        Color.red
                    );

                    EditorGUI.DrawRect(
                        new Rect(
                            previewRect.x + captureBox.x * scale,
                            previewRect.y + captureBox.yMax * scale,
                            captureBox.width * scale,
                            1
                        ),
                        Color.red
                    );
                }
            }
        }
    }
}