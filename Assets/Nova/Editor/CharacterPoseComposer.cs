using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        private const string StandingsFolderPrefix = "Assets/Resources/";
        private const string DefaultStandingsFolder = "Assets/Resources/Standings";

        private string imageFolder = DefaultStandingsFolder;
        private bool uncropped;
        private string newImageFolder = DefaultStandingsFolder;
        private bool newUncropped;
        private int selectedCharacterIndex = -1;
        private GameCharacterController selectedCharacter;
        private string poseString;
        private int selectedPoseIndex = -1;
        private string selectedPoseString;
        private readonly List<Layer> layers = new List<Layer>();
        private ReorderableList reorderableList;
        private bool useCaptureBox;
        private RectInt captureBox = new RectInt(0, 0, 400, 400);
        private float previewScale = 2.0f;

        private GameObject root;
        private CompositeSpriteMerger merger;
        private Camera renderCamera;
        private bool dirty;
        private RenderTexture previewTexture;
        private List<SpriteWithOffset> previewSprites;
        private Rect previewBounds;

        private readonly EditorLuaRuntime lua = new EditorLuaRuntime();

        private void OnEnable()
        {
            root = CompositeSpriteMerger.InstantiateSimpleSpriteMerger("CharacterPoseComposer", out renderCamera,
                out merger);

            reorderableList = new ReorderableList(layers, typeof(Layer), true, true, true, true);
            reorderableList.drawHeaderCallback += DrawHeader;
            reorderableList.drawElementCallback += DrawElement;
            reorderableList.onAddCallback += AddItem;
            reorderableList.onRemoveCallback += RemoveItem;
            reorderableList.onReorderCallback += ReorderItem;

            lua.Init();
        }

        private void OnDisable()
        {
            reorderableList.drawHeaderCallback -= DrawHeader;
            reorderableList.drawElementCallback -= DrawElement;
            reorderableList.onAddCallback -= AddItem;
            reorderableList.onRemoveCallback -= RemoveItem;
            reorderableList.onReorderCallback -= ReorderItem;

            DestroyImmediate(root);
            if (previewTexture != null)
            {
                DestroyImmediate(previewTexture);
            }

            lua.Dispose();
        }

        private static void DrawHeader(Rect rect)
        {
            GUI.Label(rect, "Layers");
        }

        private void DrawElement(Rect rect, int index, bool active, bool focused)
        {
            var item = layers[index];
            var guids = AssetDatabase.FindAssets(uncropped ? "t:Sprite" : "t:SpriteWithOffset", new[] {imageFolder});

            if (guids.Length == 0)
            {
                GUI.Label(rect, "No sprite found");
                return;
            }

            if (EditorGUI.DropdownButton(rect, new GUIContent(item.name ?? "Sprite"), FocusType.Keyboard))
            {
                var menu = new GenericMenu();
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var layerName = Path.GetFileNameWithoutExtension(path);

                    SpriteWithOffset sprite;
                    if (uncropped)
                    {
                        // Use Sprite
                        var _sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                        if (_sprite == null)
                        {
                            continue;
                        }

                        sprite = CreateInstance<SpriteWithOffset>();
                        sprite.sprite = _sprite;
                        sprite.offset = Vector3.zero;
                    }
                    else
                    {
                        // Use SpriteWithOffset
                        sprite = AssetDatabase.LoadAssetAtPath<SpriteWithOffset>(Path.ChangeExtension(path, "asset"));
                        if (sprite == null)
                        {
                            continue;
                        }
                    }

                    menu.AddItem(new GUIContent(CreateSubMenu(layerName)), false, () =>
                    {
                        item.name = layerName;
                        item.sprite = sprite;
                        dirty = true;
                    });
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

        private bool RenderLayers(ref Rect previewRect, float scale)
        {
            renderCamera.targetTexture = null;
            if (previewSprites == null || dirty)
            {
                previewSprites = layers.Where(p => !string.IsNullOrEmpty(p.name)).Select(p => p.sprite).ToList();
                previewBounds = CompositeSpriteMerger.GetMergedSize(previewSprites);
                poseString = CompositeSpriteController.ArrayToPose(layers.Select(x => x.name));
                if (poseString != selectedPoseString)
                {
                    selectedPoseIndex = -1;
                }
            }

            if (previewSprites.Count == 0)
            {
                return false;
            }

            previewRect.size =
                Utils.GetContentSize(previewRect.size, previewBounds.width / previewBounds.height);
            var previewWidth = Mathf.Max(1, (int)(previewRect.width * scale));
            var previewHeight = Mathf.Max(1, (int)(previewRect.height * scale));

            if (previewTexture == null || previewTexture.width != previewWidth ||
                previewTexture.height != previewHeight)
            {
                if (previewTexture != null)
                {
                    DestroyImmediate(previewTexture);
                }

                previewTexture = new RenderTexture(previewWidth, previewHeight, 0, RenderTextureFormat.ARGB32);
                dirty = true;
            }

            if (dirty)
            {
                previewTexture.Clear();
                merger.RenderToTexture(previewSprites, renderCamera, previewBounds, previewTexture);
            }

            return true;
        }

        private IEnumerable<Layer> PoseToLayers(string pose)
        {
            return CompositeSpriteController.PoseToArray(pose)
                .Select(x => new Layer
                {
                    name = x,
                    sprite = AssetDatabase.LoadAssetAtPath<SpriteWithOffset>(
                        Path.ChangeExtension(Path.Combine(imageFolder, x), "asset"))
                });
        }

        private void LoadPoseString()
        {
            layers.Clear();
            layers.AddRange(PoseToLayers(poseString));
            dirty = true;
        }

        // You may edit these according to your conventions
        private static string[] SubMenus = {"eyebrow", "eye", "mouth"};

        private string CreateSubMenu(string s)
        {
            foreach (var prefix in SubMenus)
            {
                s = Regex.Replace(s, $"^{prefix}_", $"{prefix}/");
            }

            return s;
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);

            // Begin left panel
            GUILayout.BeginVertical(GUILayout.Width(360));
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Character Image Folder", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            newUncropped = GUILayout.Toggle(newUncropped, "Uncropped");
            GUILayout.EndHorizontal();

            newImageFolder = EditorGUILayout.TextField(newImageFolder);

            GUILayout.BeginHorizontal();
            var refreshFolder = false;
            if (GUILayout.Button("Load Selected Folder", GUILayout.Width(180)))
            {
                newImageFolder = EditorUtils.GetSelectedDirectory();
                newUncropped = imageFolder.ToLower().Contains("uncrop");
                refreshFolder = true;
            }

            refreshFolder |= GUILayout.Button("Refresh Folder", GUILayout.Width(180));
            GUILayout.EndHorizontal();

            var characters = GameObject.FindGameObjectsWithTag("Character")
                .Select(go => go.GetComponent<GameCharacterController>())
                .Where(x => x != null).ToList();
            var menus = characters.Select(x => x.luaGlobalName).ToArray();
            var oldSelectedCharacterIndex = selectedCharacterIndex;

            selectedCharacterIndex = EditorGUILayout.Popup("Select Character", selectedCharacterIndex, menus);
            selectedCharacter = characters.Any() && selectedCharacterIndex >= 0
                ? characters[selectedCharacterIndex]
                : null;

            if (selectedCharacter != null && selectedCharacterIndex != oldSelectedCharacterIndex)
            {
                newImageFolder = StandingsFolderPrefix + selectedCharacter.imageFolder;
                newUncropped = false;
                refreshFolder = true;
            }

            if (refreshFolder)
            {
                imageFolder = newImageFolder;
                uncropped = newUncropped;
                if (selectedCharacter != null && imageFolder != StandingsFolderPrefix + selectedCharacter.imageFolder)
                {
                    selectedCharacter = null;
                    selectedCharacterIndex = -1;
                }

                layers.Clear();
                selectedPoseIndex = -1;
                dirty = true;
            }

            if (selectedCharacter != null)
            {
                var characterName = selectedCharacter.luaGlobalName;
                lua.Reload();
                var poses = lua.GetAllPosesByName(characterName);
                var oldSelectedPose = selectedPoseIndex;

                selectedPoseIndex = EditorGUILayout.Popup("Select Pose", selectedPoseIndex,
                    poses.Select(CreateSubMenu).ToArray());

                if (poses.Any() && selectedPoseIndex >= 0 && selectedPoseIndex != oldSelectedPose)
                {
                    selectedPoseString = lua.GetPoseByName(characterName, poses[selectedPoseIndex]);
                    if (poseString != selectedPoseString)
                    {
                        poseString = selectedPoseString;
                        LoadPoseString();
                    }
                }
            }

            GUILayout.Space(20);

            reorderableList.DoLayoutList();

            GUILayout.Space(20);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pose String", EditorStyles.boldLabel);
            if (GUILayout.Button("Copy", GUILayout.Width(120)))
            {
                EditorGUIUtility.systemCopyBuffer = poseString;
            }

            GUILayout.EndHorizontal();

            poseString = EditorGUILayout.TextField(poseString);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Pose String", GUILayout.Width(180)))
            {
                LoadPoseString();
            }

            if (GUILayout.Button("Refresh Preview", GUILayout.Width(180)))
            {
                dirty = true;
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(20);

            previewScale = EditorGUILayout.FloatField("Preview Scale", previewScale);
            useCaptureBox = GUILayout.Toggle(useCaptureBox, "Use Capture Box");
            if (useCaptureBox)
            {
                captureBox = EditorGUILayout.RectIntField(captureBox);
            }

            GUILayout.EndVertical();
            // End left panel

            if (dirty)
            {
                GUI.FocusControl(null);
            }

            GUILayout.Space(20);

            // Begin right panel
            GUILayout.BeginVertical();

            GUILayout.Space(8);

            GUILayout.Label("Preview", EditorStyles.boldLabel);

            var previewRect =
                EditorGUILayout.GetControlRect(false, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            var shouldDraw = RenderLayers(ref previewRect, previewScale);
            dirty = false;
            if (shouldDraw)
            {
                EditorGUI.DrawTextureTransparent(previewRect, previewTexture);
                if (useCaptureBox)
                {
                    var scale = previewRect.width / (previewBounds.width * previewSprites[0].sprite.pixelsPerUnit);
                    EditorUtils.DrawPreviewCaptureFrame(previewRect, captureBox.ToRect(), scale, false, Color.red);
                }
            }

            GUILayout.EndVertical();
            // End right panel

            GUILayout.EndHorizontal();
        }
    }
}
