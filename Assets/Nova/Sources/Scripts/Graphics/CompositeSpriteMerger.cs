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
                if (sprites[i] != null)
                {
                    layers[i].sprite = sprites[i].sprite;
                    layers[i].transform.localPosition = sprites[i].offset;
                    layers[i].enabled = true;
                }
                else
                {
                    layers[i].enabled = false;
                }
            }
        }

        public void SetTextures(CompositeSpriteMerger other)
        {
            EnsureLayers(other.spriteCount);
            for (var i = 0; i < other.spriteCount; i++)
            {
                layers[i].sprite = other.layers[i].sprite;
                layers[i].transform.localPosition = other.layers[i].transform.localPosition;
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

        public RenderTexture RenderToTexture(IReadOnlyList<SpriteWithOffset> sprites, Camera renderCamera)
        {
            // Debug.Log("render to texture");
            SetTextures(sprites);
            var bounds = CompositeSpriteMerger.GetMergedSize(sprites);
            var pixelsPerUnit = sprites[0].sprite.pixelsPerUnit;
            var size = bounds.size * pixelsPerUnit;
            var renderTexture = new RenderTexture((int)size.x, (int)size.y, 0, RenderTextureFormat.ARGB32);

            renderCamera.targetTexture = renderTexture;
            renderCamera.orthographicSize = bounds.size.y / 2 * renderCamera.transform.lossyScale.y;
            renderCamera.transform.localPosition = new Vector3(bounds.center.x, bounds.center.y, 0);

            renderCamera.Render();
            ClearTextures();

            return renderTexture;
        }

        private static Rect GetMergedSize(IEnumerable<SpriteWithOffset> spriteList)
        {
            var sprites = spriteList.Where(x => x != null).ToList();
            if (!sprites.Any())
            {
                return Rect.zero;
            }

            var xmin = float.MaxValue;
            var ymin = float.MaxValue;
            var xmax = float.MinValue;
            var ymax = float.MinValue;
            foreach (var sprite in sprites)
            {
                var b = sprite.sprite.bounds;
                var o = sprite.offset;
                xmin = Mathf.Min(xmin, b.min.x + o.x);
                ymin = Mathf.Min(ymin, b.min.y + o.y);
                xmax = Mathf.Max(xmax, b.max.x + o.x);
                ymax = Mathf.Max(ymax, b.max.y + o.y);
            }

            return Rect.MinMaxRect(xmin, ymin, xmax, ymax);
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
            renderCamera.cullingMask = 1 << CompositeSpriteMerger.MergerLayer;
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
