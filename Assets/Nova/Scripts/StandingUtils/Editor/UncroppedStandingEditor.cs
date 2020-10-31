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

            foreach (var spritePath in EditorUtils.PathOfSelectedSprites())
            {
                var go = new GameObject("Standing Component");
                var spriteRenderer = go.AddComponent<SpriteRenderer>();
                var sprite = spriteRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                var cropper = go.AddComponent<SpriteCropper>();
                cropper.cropRect = new RectInt(0, 0, sprite.texture.width, sprite.texture.height);
                go.transform.SetParent(parent.transform);
                ResetTransform(go.transform);
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

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var standing = target as UncroppedStanding;

            if (GUILayout.Button("Auto Crop All"))
            {
                foreach (var cropper in standing.GetComponentsInChildren<SpriteCropper>())
                {
                    SpriteCropperEditor.AutoCrop(cropper);
                }
            }

            if (GUILayout.Button("Write Cropped Textures"))
            {
                WriteCropResult(standing);
            }

            if (GUILayout.Button("Generate Metadata"))
            {
                GenerateMetaData(standing);
            }
        }

        private static void WriteCropResult(UncroppedStanding standing)
        {
            foreach (var cropper in standing.GetComponentsInChildren<SpriteCropper>())
            {
                WriteCropResult(standing, cropper);
            }

            AssetDatabase.Refresh();
        }

        private static void WriteCropResult(UncroppedStanding standing, SpriteCropper cropper)
        {
            var uncropped = cropper.sprite.texture;
            var cropRect = cropper.cropRect;
            var cropped = new Texture2D(cropRect.width, cropRect.height, TextureFormat.RGBA32, false);
            var pixels = uncropped.GetPixels(cropRect.x, cropRect.y, cropRect.width, cropRect.height);
            cropped.SetPixels(pixels);
            cropped.Apply();
            var bytes = cropped.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(standing.absoluteOutputDirectory, cropper.sprite.name + ".png"), bytes);
        }

        private static void GenerateMetaData(UncroppedStanding standing)
        {
            foreach (var cropper in standing.GetComponentsInChildren<SpriteCropper>())
            {
                GenerateMetaData(standing, cropper);
            }
        }

        private static void GenerateMetaData(UncroppedStanding standing, SpriteCropper cropper)
        {
            var cropRect = cropper.cropRect;
            var uncropped = cropper.sprite.texture;
            var outputDir = Path.Combine("Assets", standing.outputDirectory);
            var cropped = AssetDatabase.LoadAssetAtPath<Sprite>(
                Path.Combine(outputDir, cropper.sprite.name + ".png"));
            var meta = CreateInstance<SpriteWithOffset>();
            meta.offset = (cropRect.center - new Vector2(uncropped.width, uncropped.height) / 2.0f) /
                          cropper.sprite.pixelsPerUnit;
            meta.sprite = cropped;
            AssetDatabase.CreateAsset(meta, Path.Combine(outputDir, cropper.sprite.name + ".asset"));
        }
    }
}