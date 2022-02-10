using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nova.Editor
{
    [CustomEditor(typeof(UncroppedStanding))]
    public class UncroppedStandingEditor : UnityEditor.Editor
    {
        private static void ResetTransform(Transform transform)
        {
            transform.position = Vector3.zero;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
        }

        [MenuItem("Assets/Create/Nova/Uncropped Standing", false)]
        public static void CreateUncroppedStandingWithSelectedSprites()
        {
            const string assetName = "UncroppedStanding";
            var parent = new GameObject(assetName);
            ResetTransform(parent.transform);
            parent.AddComponent<UncroppedStanding>();

            foreach (var spritePath in EditorUtils.GetSelectedSpritePaths())
            {
                var go = new GameObject("StandingComponent");
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

            var currentDir = EditorUtils.GetSelectedDirectory();

            PrefabUtility.SaveAsPrefabAsset(parent,
                Path.Combine(currentDir, AssetDatabase.GenerateUniqueAssetPath(assetName + ".prefab")));
            DestroyImmediate(parent);
        }

        [MenuItem("Assets/Create/Nova/Uncropped Standing", true)]
        public static bool CreateUncroppedStandingWithSelectedSpritesValidation()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(Texture2D);
        }

        private bool useCaptureBox;
        private RectInt captureBox = new RectInt(0, 0, 400, 400);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var standing = target as UncroppedStanding;

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
                foreach (var cropper in standing.GetComponentsInChildren<SpriteCropper>())
                {
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
            }

            if (GUILayout.Button("Write Cropped Textures"))
            {
                WriteCroppedTexture(standing);
            }

            if (GUILayout.Button("Generate Metadata"))
            {
                GenerateMetadata(standing);
            }
        }

        private static void WriteCroppedTexture(UncroppedStanding standing)
        {
            foreach (var cropper in standing.GetComponentsInChildren<SpriteCropper>())
            {
                WriteCroppedTexture(standing, cropper);
            }

            AssetDatabase.Refresh();
        }

        private static void WriteCroppedTexture(UncroppedStanding standing, SpriteCropper cropper)
        {
            var cropRect = cropper.cropRect;
            var cropped = new Texture2D(cropRect.width, cropRect.height, TextureFormat.RGBA32, false);
            var texture = cropper.sprite.texture;
            var pixels = texture.GetPixels(cropRect.x, cropRect.y, cropRect.width, cropRect.height);
            cropped.SetPixels(pixels);
            cropped.Apply();

            var bytes = cropped.EncodeToPNG();
            var fileName = cropper.sprite.name + ".png";
            var absoluteOutputPath = Path.Combine(standing.absoluteOutputDirectory, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(absoluteOutputPath));
            File.WriteAllBytes(absoluteOutputPath, bytes);

            var assetPath = Path.Combine(standing.outputDirectory, fileName);
            AssetDatabase.ImportAsset(assetPath);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }
        }

        private static void GenerateMetadata(UncroppedStanding standing)
        {
            foreach (var cropper in standing.GetComponentsInChildren<SpriteCropper>())
            {
                GenerateMetadata(standing, cropper);
            }
        }

        private static void GenerateMetadata(UncroppedStanding standing, SpriteCropper cropper)
        {
            var meta = CreateInstance<SpriteWithOffset>();
            meta.offset = (cropper.cropRect.center - cropper.boundRect.center) / cropper.sprite.pixelsPerUnit;
            var path = Path.Combine(standing.outputDirectory, cropper.sprite.name + ".png");
            meta.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            AssetDatabase.CreateAsset(meta, Path.Combine(standing.outputDirectory, cropper.sprite.name + ".asset"));
        }
    }
}