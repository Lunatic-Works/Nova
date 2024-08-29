using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nova.Editor
{
    [CustomEditor(typeof(UncroppedSprites))]
    public class UncroppedSpritesEditor : UnityEditor.Editor
    {
        private static void ResetTransform(Transform transform)
        {
            transform.position = Vector3.zero;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
        }

        [MenuItem("Assets/Create/Nova/Uncropped Sprites")]
        public static void CreateUncroppedSprites()
        {
            var dir = EditorUtils.GetSelectedDirectory();
            var guids = AssetDatabase.FindAssets("t:GameObject", new[] {dir});
            var fileName = "UncroppedSprites.prefab";
            var outputDirectory = "";
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!go.TryGetComponent<UncroppedSprites>(out var oldSprites))
                {
                    continue;
                }

                fileName = Path.GetFileName(path);
                outputDirectory = oldSprites.outputDirectory;
                break;
            }

            var parent = new GameObject("UncroppedSprites");
            ResetTransform(parent.transform);
            var sprites = parent.AddComponent<UncroppedSprites>();
            sprites.outputDirectory = outputDirectory;

            foreach (var spritePath in EditorUtils.GetSelectedSpritePaths().OrderBy(x => x))
            {
                var go = new GameObject("Layer");
                go.transform.SetParent(parent.transform);
                ResetTransform(go.transform);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                var spriteRenderer = go.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = sprite;
                var texture = sprite.texture;
                var cropper = go.AddComponent<SpriteCropper>();
                cropper.boundRect = new RectInt(0, 0, texture.width, texture.height);
                cropper.cropRect = new RectInt(0, 0, texture.width, texture.height);
            }

            PrefabUtility.SaveAsPrefabAsset(parent, Path.Combine(dir, fileName));
            DestroyImmediate(parent);
        }

        [MenuItem("Assets/Create/Nova/Uncropped Sprites", true)]
        public static bool CreateUncroppedSpritesValidation()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(Texture2D);
        }

        [MenuItem("Assets/Create/Nova/Sprite With Offset")]
        public static void CreateSpriteWithOffset()
        {
            var dir = EditorUtils.GetSelectedDirectory();
            var paths = EditorUtils.GetSelectedSpritePaths();
            foreach (var spritePath in paths)
            {
                var meta = CreateInstance<SpriteWithOffset>();
                meta.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                var metaPath = Path.Combine(dir, Path.GetFileNameWithoutExtension(spritePath) + ".asset");
                AssetDatabase.CreateAsset(meta, metaPath);
            }
        }

        [MenuItem("Assets/Create/Nova/Sprite With Offset", true)]
        public static bool CreateSpriteWithOffsetValidation()
        {
            return CreateUncroppedSpritesValidation();
        }

        private bool useCaptureBox;
        private RectInt captureBox = new RectInt(0, 0, 400, 400);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var sprites = target as UncroppedSprites;

            useCaptureBox = GUILayout.Toggle(useCaptureBox, "Use Capture Box");
            if (useCaptureBox)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Capture Box");
                captureBox = EditorGUILayout.RectIntField(captureBox);
                GUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Auto Crop All"))
            {
                var croppers = sprites.GetComponentsInChildren<SpriteCropper>();
                int count = 0;
                foreach (var cropper in croppers)
                {
                    ++count;
                    EditorUtility.DisplayCancelableProgressBar(
                        "Auto Cropping Sprites", cropper.sprite.name, (float)count / croppers.Length);

                    var texture = cropper.sprite.texture;
                    if (useCaptureBox)
                    {
                        cropper.boundRect.xMin = captureBox.xMin;
                        cropper.boundRect.yMin = texture.height - captureBox.yMax;
                        cropper.boundRect.size = captureBox.size;
                    }
                    else
                    {
                        cropper.boundRect.min = Vector2Int.zero;
                        cropper.boundRect.width = texture.width;
                        cropper.boundRect.height = texture.height;
                    }

                    SpriteCropperEditor.AutoCrop(cropper);
                }

                EditorUtility.ClearProgressBar();
            }

            if (GUILayout.Button("Write Cropped Textures"))
            {
                WriteCroppedTexture(sprites);
            }

            if (GUILayout.Button("Generate Metadata"))
            {
                GenerateMetadata(sprites);
            }
        }

        private static void WriteCroppedTexture(UncroppedSprites sprites)
        {
            foreach (var cropper in sprites.GetComponentsInChildren<SpriteCropper>())
            {
                WriteCroppedTexture(sprites, cropper);
            }
        }

        private static int PadToPOT(int val)
        {
            var i = 1;
            while (i < val)
            {
                i *= 2;
            }

            return i;
        }

        private static void WriteCroppedTexture(UncroppedSprites sprites, SpriteCropper cropper)
        {
            var cropRect = cropper.cropRect;
            var width = PadToPOT(cropRect.width);
            var height = PadToPOT(cropRect.height);
            var cropped = new Texture2D(width, height, TextureFormat.RGBA32, false);

            var background = Enumerable.Repeat(Color.clear, width * height).ToArray();
            cropped.SetPixels(background);

            var texture = cropper.sprite.texture;
            var pixels = texture.GetPixels(cropRect.x, cropRect.y, cropRect.width, cropRect.height);
            var x = (width - cropRect.width) / 2;
            var y = (height - cropRect.height) / 2;
            cropped.SetPixels(x, y, cropRect.width, cropRect.height, pixels);

            cropped.Apply();

            var fileName = cropper.sprite.name + ".png";
            var absoluteOutputPath = Path.Combine(sprites.absoluteOutputDirectory, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(absoluteOutputPath));
            File.WriteAllBytes(absoluteOutputPath, cropped.EncodeToPNG());
            Utils.DestroyObject(cropped);

            var assetPath = Path.Combine(sprites.outputDirectory, fileName);
            AssetDatabase.ImportAsset(assetPath);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            var needReimport = false;
            if (importer.textureType != TextureImporterType.Sprite ||
                !importer.mipmapEnabled ||
                importer.filterMode != FilterMode.Trilinear ||
                importer.anisoLevel < 1)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Trilinear;
                importer.anisoLevel = 1;
                needReimport = true;
            }

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteMeshType != SpriteMeshType.FullRect)
            {
                settings.spriteMeshType = SpriteMeshType.FullRect;
                needReimport = true;
            }

            if (needReimport)
            {
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        private static void GenerateMetadata(UncroppedSprites sprites)
        {
            foreach (var cropper in sprites.GetComponentsInChildren<SpriteCropper>())
            {
                GenerateMetadata(sprites, cropper);
            }
        }

        private static void GenerateMetadata(UncroppedSprites sprites, SpriteCropper cropper)
        {
            var meta = CreateInstance<SpriteWithOffset>();
            meta.offset = (cropper.cropRect.center - cropper.boundRect.center) / cropper.sprite.pixelsPerUnit;
            var path = Path.Combine(sprites.outputDirectory, cropper.sprite.name + ".png");
            meta.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            AssetDatabase.CreateAsset(meta, Path.Combine(sprites.outputDirectory, cropper.sprite.name + ".asset"));
        }
    }
}
