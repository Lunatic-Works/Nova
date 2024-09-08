using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nova
{
    public class CompositeSpriteMerger : MonoBehaviour
    {
        private const int MergerLayer = 16;

        private readonly List<SpriteRenderer> layers = new List<SpriteRenderer>();

        public int spriteCount { get; private set; }

        private void EnsureLayers(int count)
        {
            for (int i = layers.Count; i < count; i++)
            {
                var go = new GameObject("MergingSprite" + i);
                go.transform.SetParent(transform, false);
                go.layer = MergerLayer;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = i;
                layers.Add(sr);
            }

            for (var i = 0; i < layers.Count; i++)
            {
                layers[i].gameObject.SetActive(i < count);
                if (i >= count)
                {
                    layers[i].sprite = null;
                }
            }

            spriteCount = count;
        }

        public void SetTextures(IReadOnlyList<SpriteWithOffset> sprites)
        {
            if (sprites == null)
            {
                EnsureLayers(0);
                return;
            }

            EnsureLayers(sprites.Count);
            for (var i = 0; i < sprites.Count; i++)
            {
                var layer = layers[i];
                var sprite = sprites[i];
                if (sprite != null)
                {
                    layer.sprite = sprite.sprite;
                    var layerTransform = layer.transform;
                    layerTransform.localPosition = sprite.offset;
                    layerTransform.localScale = sprite.scale;
                    layer.enabled = true;
                }
                else
                {
                    layer.enabled = false;
                }
            }
        }

        public void SetTextures(CompositeSpriteMerger other)
        {
            var otherTransform = other.transform;
            transform.localPosition = otherTransform.localPosition;
            transform.localScale = otherTransform.localScale;

            EnsureLayers(other.spriteCount);
            for (var i = 0; i < other.spriteCount; i++)
            {
                var layer = layers[i];
                var otherLayer = other.layers[i];
                layer.sprite = otherLayer.sprite;
                var layerTransform = layer.transform;
                var otherLayerTransform = otherLayer.transform;
                layerTransform.localPosition = otherLayerTransform.localPosition;
                layerTransform.localScale = otherLayerTransform.localScale;
            }
        }

        private void ClearTextures()
        {
            SetTextures(Array.Empty<SpriteWithOffset>());
        }

        public void Render(CommandBuffer cmd, int rt)
        {
            cmd.SetRenderTarget(rt);
            cmd.ClearRenderTarget(true, true, Color.clear);
            if (!isActiveAndEnabled)
            {
                return;
            }

            for (var i = 0; i < spriteCount; i++)
            {
                cmd.DrawRenderer(layers[i], layers[i].sharedMaterial);
            }
        }

        public void RenderToTexture(IReadOnlyList<SpriteWithOffset> sprites, Camera renderCamera, Rect bounds,
            RenderTexture target)
        {
            SetTextures(sprites);
            var height = Mathf.Max(bounds.height, bounds.width / target.width * target.height);

            renderCamera.targetTexture = target;
            renderCamera.orthographicSize = height / 2 * renderCamera.transform.lossyScale.y;
            renderCamera.transform.localPosition = new Vector3(bounds.center.x, bounds.center.y, 0);

            renderCamera.Render();
            ClearTextures();
        }

        public RenderTexture RenderToTexture(IReadOnlyList<SpriteWithOffset> sprites, Camera renderCamera)
        {
            var bounds = GetMergedSize(sprites);
            var pixelsPerUnit = sprites[0].sprite.pixelsPerUnit;
            var size = bounds.size * pixelsPerUnit;
            var renderTexture = new RenderTexture((int)size.x, (int)size.y, 0, RenderTextureFormat.ARGB32);

            RenderToTexture(sprites, renderCamera, bounds, renderTexture);
            return renderTexture;
        }

        public static Rect GetMergedSize(IEnumerable<SpriteWithOffset> spriteList)
        {
            var sprites = spriteList.Where(x => x != null).ToList();
            if (!sprites.Any())
            {
                return Rect.zero;
            }

            var xMin = float.MaxValue;
            var yMin = float.MaxValue;
            var xMax = float.MinValue;
            var yMax = float.MinValue;
            foreach (var sprite in sprites)
            {
                var bounds = sprite.sprite.bounds;
                var center = bounds.center + sprite.offset;
                var extents = bounds.extents.CloneScale(sprite.scale);
                xMin = Mathf.Min(xMin, center.x - extents.x);
                yMin = Mathf.Min(yMin, center.y - extents.y);
                xMax = Mathf.Max(xMax, center.x + extents.x);
                yMax = Mathf.Max(yMax, center.y + extents.y);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

#if UNITY_EDITOR
        public static GameObject InstantiateSimpleSpriteMerger(string name, out Camera renderCamera,
            out CompositeSpriteMerger merger)
        {
            var root = new GameObject(name)
            {
                hideFlags = HideFlags.DontSave
            };
            merger = root.Ensure<CompositeSpriteMerger>();
            merger.runInEditMode = true;

            var camera = new GameObject("Camera");
            camera.transform.SetParent(root.transform, false);
            renderCamera = camera.Ensure<Camera>();
            renderCamera.cullingMask = 1 << MergerLayer;
            renderCamera.orthographic = true;
            renderCamera.enabled = false;
            renderCamera.nearClipPlane = -1;
            renderCamera.farClipPlane = 1;
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = Color.clear;

            return root;
        }
#endif
    }
}
