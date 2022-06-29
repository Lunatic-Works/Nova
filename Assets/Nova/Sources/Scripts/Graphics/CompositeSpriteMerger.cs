using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Nova.URP;
using UnityEngine.Rendering;

namespace Nova
{
    public class CompositeSpriteMerger : MonoBehaviour
    {
        public CompositeSpriteController controller;
        public string textureName;
        private readonly List<SpriteRenderer> layers = new List<SpriteRenderer>();
        private string mergerName => controller.gameObject.name + gameObject.name;

        public int spriteCount { get; private set; } = 0;

        private void EnsureLayers(int count)
        {
            for (int i = layers.Count; i < count; i++)
            {
                var go = new GameObject("MergingSprite" + i);
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                go.layer = CompositeSpriteController.mergerLayer;
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
    }
}
