using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nova.Editor
{
    class ImageGroupCapturer : IDisposable
    {
        private const int SnapshotWidth = ImageGroupEditor.SnapshotWidth;
        private const int SnapshotHeight = ImageGroupEditor.SnapshotHeight;
        private const string ResourcesFolderName = ImageGroupEditor.ResourcesFolderName;

        private static string GetAssetFullPath(UnityEngine.Object asset)
        {
            return Path.Combine(Path.GetDirectoryName(Application.dataPath), AssetDatabase.GetAssetPath(asset));
        }

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

        public RenderTexture renderTexture { get; private set; }

        private readonly RenderTexture snapshotTexture;
        private readonly Texture2D writeTexture;
        private readonly GameObject root;
        private readonly CompositeSpriteMerger merger;
        private readonly Camera renderCamera;

        public ImageGroupCapturer()
        {
            root = CompositeSpriteMerger.InstantiateSimpleSpriteMerger("ImageGroupCapturer", out renderCamera,
                out merger);
            snapshotTexture = new RenderTexture(SnapshotWidth, SnapshotHeight, 0);
            writeTexture = new Texture2D(SnapshotWidth, SnapshotHeight);
        }

        // need to be called when not used anymore
        public void OnDestroy()
        {
            if (renderTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }

            if (snapshotTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(snapshotTexture);
            }

            if (writeTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(writeTexture);
            }

            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        public void Dispose()
        {
            OnDestroy();
        }

        private byte[] GetSnapshotPNGData()
        {
            var oldRt = RenderTexture.active;
            RenderTexture.active = snapshotTexture;
            writeTexture.ReadPixels(new Rect(0, 0, SnapshotWidth, SnapshotHeight), 0, 0);
            writeTexture.Apply();
            RenderTexture.active = oldRt;
            return writeTexture.EncodeToPNG();
        }

        // return resourcePath to get an output directory
        private bool DrawComposite(ImageEntry entry, out string resourcePath)
        {
            var sprites = CompositeSpriteController.LoadSprites(entry.resourcePath, entry.poseString);
            if (!sprites.Any() || sprites.Contains(null))
            {
                resourcePath = "";
                return false;
            }

            resourcePath = GetResourcesFolder(GetAssetFullPath(sprites[0]));
            if (renderTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }

            renderTexture = merger.RenderToTexture(sprites, renderCamera);
            return true;
        }

        public bool DrawComposite(ImageEntry entry)
        {
            return DrawComposite(entry, out _);
        }

        private void GenerateSnapshot(ImageEntry entry)
        {
            if (entry == null) return;
            Texture tex;
            string resourcePath;
            if (entry.composite)
            {
                if (!DrawComposite(entry, out resourcePath)) return;
                tex = renderTexture;
            }
            else
            {
                var sprite = Resources.Load<Sprite>(entry.resourcePath);
                if (sprite == null) return;
                tex = sprite.texture;
                resourcePath = GetResourcesFolder(GetAssetFullPath(sprite));
            }

            Graphics.Blit(tex, snapshotTexture, entry.snapshotScale, entry.snapshotOffset);
            var data = GetSnapshotPNGData();
            var snapshotFullPath = Path.Combine(resourcePath, entry.snapshotResourcePath + ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotFullPath));
            File.WriteAllBytes(snapshotFullPath, data);
        }

        public void GenerateSnapshot(ImageGroup group)
        {
            if (group == null || group.entries.Count <= 0) return;
            // Need to generate everything as the preview is the first unlocked snapshot
            foreach (var entry in group.entries)
            {
                GenerateSnapshot(entry);
            }
        }
    }
}
