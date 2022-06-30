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
        private class Layer
        {
            public string name;
            public SpriteWithOffset sprite;
        }

        [MenuItem("Nova/Character Pose Composer")]
        public static void ShowWindow()
        {
            GetWindow(typeof(CharacterPoseComposer), false, "Character Pose Composer");
        }

        private string imageFolder;
        private bool uncropped;

        private readonly List<Layer> layers = new List<Layer>();
        private ReorderableList reorderableList;
        private bool dirty;

        private GameObject root;
        private CompositeSpriteMerger merger;
        private Camera renderCamera;
        private RenderTexture renderTexture;

        private bool useCaptureBox;
        private RectInt captureBox = new RectInt(0, 0, 400, 400);
        private string captureDest;

        private void OnEnable()
        {
            root = new GameObject("CharacterPoseComposer")
            {
                hideFlags = HideFlags.DontSave
            };
            merger = root.Ensure<CompositeSpriteMerger>();
            merger.runInEditMode = true;

            var camera = new GameObject("camera");
            camera.transform.SetParent(root.transform);
            renderCamera = camera.Ensure<Camera>();
            renderCamera.cullingMask = 1 << CompositeSpriteMerger.mergerLayer;
            renderCamera.orthographic = true;
            renderCamera.enabled = false;
            renderCamera.nearClipPlane = -1;
            renderCamera.farClipPlane = 1;
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = Color.clear;

            reorderableList = new ReorderableList(layers, typeof(Layer), true, true, true, true);
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

            DestroyImmediate(renderTexture);
            DestroyImmediate(root);
        }

        private static void DrawHeader(Rect rect)
        {
            GUI.Label(rect, "Layers");
        }

        private void DrawElement(Rect rect, int index, bool active, bool focused)
        {
            var item = layers[index];
            var paths = AssetDatabase.FindAssets(uncropped ? "t:Sprite" : "t:SpriteWithOffset", new[] { imageFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToList();

            if (paths.Count == 0)
            {
                GUI.Label(rect, "No sprite found");
                return;
            }

            if (EditorGUI.DropdownButton(
                new Rect(rect.x + rect.width / 3, rect.y, rect.width * 2 / 3, rect.height),
                new GUIContent(item.name ?? "Sprite"),
                FocusType.Keyboard
            ))
            {
                var menu = new GenericMenu();

                foreach (var path in paths)
                {
                    var layerName = Path.GetFileNameWithoutExtension(path);

                    if (uncropped)
                    {
                        // Use Sprite
                        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                        if (sprite == null)
                        {
                            continue;
                        }

                        var so = CreateInstance<SpriteWithOffset>();
                        so.sprite = sprite;
                        so.offset = Vector3.zero;

                        menu.AddItem(new GUIContent(layerName), false, () =>
                        {
                            item.name = layerName;
                            item.sprite = so;
                            dirty = true;
                        });
                    }
                    else
                    {
                        // Use SpriteWithOffset
                        var sprite =
                            AssetDatabase.LoadAssetAtPath<SpriteWithOffset>(Path.ChangeExtension(path, "asset"));
                        if (sprite == null)
                        {
                            continue;
                        }

                        menu.AddItem(new GUIContent(layerName), false, () =>
                        {
                            item.name = layerName;
                            item.sprite = sprite;
                            dirty = true;
                        });
                    }
                }

                menu.ShowAsContext();
            }
        }

        private void AddItem(ReorderableList list)
        {
            layers.Add(new Layer());
            dirty = true;
        }

        private void RemoveItem(ReorderableList list)
        {
            layers.RemoveAt(list.index);
            dirty = true;
        }

        private void ReorderItem(ReorderableList list)
        {
            dirty = true;
        }

        private static string LayersToLuaTable(IEnumerable<Layer> layers)
        {
            var luaTable = layers
                .Where(p => !string.IsNullOrEmpty(p.name))
                .Select(p => "'" + p.name + "'")
                .Aggregate((r, s) => r + ", " + s);
            luaTable = "{" + luaTable + "}";
            return luaTable;
        }

        private void OnGUI()
        {
            imageFolder = EditorGUILayout.TextField("Character Image Folder", imageFolder);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Selected Folder"))
            {
                imageFolder = EditorUtils.GetSelectedDirectory();
                uncropped = imageFolder.ToLower().Contains("uncrop");
                layers.Clear();
                dirty = true;
            }

            uncropped = GUILayout.Toggle(uncropped, "Uncropped");
            GUILayout.EndHorizontal();

            reorderableList.DoLayoutList();

            if (GUILayout.Button("Refresh"))
            {
                dirty = true;
            }

            if (dirty)
            {
                dirty = false;

                renderCamera.targetTexture = null;
                if (renderTexture != null)
                {
                    DestroyImmediate(renderTexture);
                }

                var sprites = layers.Where(p => !string.IsNullOrEmpty(p.name)).Select(p => p.sprite).ToList();
                if (sprites.Count == 0)
                {
                    renderTexture = null;
                }
                else
                {
                    merger.SetTextures(sprites);
                    var bounds = CompositeSpriteMerger.GetMergedSize(sprites);
                    var pixelsPerUnit = sprites[0].sprite.pixelsPerUnit;
                    var size = bounds.size * pixelsPerUnit;
                    renderTexture = new RenderTexture((int)size.x, (int)size.y, 0, RenderTextureFormat.ARGB32);

                    renderCamera.targetTexture = renderTexture;
                    renderCamera.orthographicSize = bounds.size.y / 2;
                    renderCamera.transform.localPosition = new Vector3(bounds.center.x, bounds.center.y, 0);

                    renderCamera.Render();
                }
            }

            if (renderTexture == null)
            {
                return;
            }

            GUILayout.Label("Composed Pose Lua Table", EditorStyles.boldLabel);
            var luaTable = LayersToLuaTable(layers);
            EditorGUILayout.SelectableLabel(luaTable);
            if (GUILayout.Button("Copy"))
            {
                EditorGUIUtility.systemCopyBuffer = luaTable;
            }

            GUILayout.Label("Preview", EditorStyles.boldLabel);

            useCaptureBox = GUILayout.Toggle(useCaptureBox, "Use Capture Box");
            if (useCaptureBox)
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
                    RenderTexture.active = renderTexture;
                    tex.ReadPixels(new Rect(captureBox.x, captureBox.y, captureBox.width, captureBox.height), 0, 0);
                    RenderTexture.active = null;
                    tex.Apply();

                    var absoluteCaptureDest = Path.Combine(Path.GetDirectoryName(Application.dataPath), captureDest);
                    Directory.CreateDirectory(Path.GetDirectoryName(absoluteCaptureDest));
                    File.WriteAllBytes(absoluteCaptureDest, tex.EncodeToPNG());
                    Utils.DestroyObject(tex);
                    EditorUtility.DisplayDialog("Capture Finished", $"Saved at {absoluteCaptureDest}", "OK");
                }

                GUILayout.EndHorizontal();
            }

            var previewRect =
                EditorGUILayout.GetControlRect(false, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            previewRect.size = Utils.GetContentSize(previewRect.size, (float)renderTexture.width / renderTexture.height);
            var scale = previewRect.width / renderTexture.width;
            EditorGUI.DrawTextureTransparent(previewRect, renderTexture);

            if (useCaptureBox)
            {
                EditorUtils.DrawPreviewCaptureFrame(previewRect, captureBox.ToRect(), scale, false, Color.red);
            }
        }
    }
}
