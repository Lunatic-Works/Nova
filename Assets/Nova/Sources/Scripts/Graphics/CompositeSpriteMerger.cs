using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nova
{
    public class CompositeSpriteMerger : MonoBehaviour
    {
        public const int MergerLayer = 16;

        private readonly List<SpriteRenderer> layers = new List<SpriteRenderer>();

        public int spriteCount { get; private set; }

        private void EnsureLayers(int count)
        {
            for (int i = layers.Count; i < count; i++)
            {
                var go = new GameObject("MergingSprite" + i);
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
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
                layers[i].sprite = sprites[i].sprite;
                layers[i].transform.localPosition = sprites[i].offset;
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

        public static Rect GetMergedSize(IReadOnlyList<SpriteWithOffset> sprites)
        {
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
    }
}
