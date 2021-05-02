using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    public class CharacterTextureMerger : MonoBehaviour
    {
        public int layer;
        public int referenceSize = 2048;
        public int pixelsPerUnit = 100;

        private LRUCache<string, RenderTexture> textureCache;
        private Camera mergeCamera;
        private List<SpriteRenderer> layers;
        private Texture empty;

        private void EnsureLayers(int count)
        {
            if (layers.Count < count)
            {
                for (int i = layers.Count; i < count; i++)
                {
                    var go = new GameObject("MergingSprite" + i);
                    go.transform.SetParent(transform);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localScale = Vector3.one;
                    go.layer = layer;
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = i;
                    layers.Add(sr);
                }
            }
        }

        private void Awake()
        {
            empty = Utils.ClearTexture;
            // 2 textures are required to avoid flickering on pose fading transition
            textureCache = new LRUCache<string, RenderTexture>(autoDestroy: true, maxSize: 4);
            layers = new List<SpriteRenderer>();
            mergeCamera = gameObject.AddComponent<Camera>();
            mergeCamera.orthographic = true;
            mergeCamera.enabled = false;
            mergeCamera.cullingMask = 1 << layer;
            mergeCamera.nearClipPlane = -1;
            mergeCamera.farClipPlane = 1;
        }

        private void OnDestroy()
        {
            foreach (var texture in textureCache.Values)
            {
                Utils.DestroyObject(texture);
            }

            foreach (var sr in layers)
            {
                Utils.DestroyObject(sr.gameObject);
            }

            Utils.DestroyObject(empty);
        }

        public Texture GetMergedTexture(List<SpriteWithOffset> sprites)
        {
            if (sprites.Count == 0)
            {
                return empty;
            }

            var key = sprites.Aggregate("", (r, s) => r + s.GetInstanceID() + ":");
            if (!textureCache.TryGetValue(key, out var result))
            {
                EnsureLayers(sprites.Count);
                for (var i = 0; i < layers.Count; i++)
                {
                    if (i < sprites.Count)
                    {
                        var s = sprites[i].sprite;
                        var offset = sprites[i].offset;
                        layers[i].sprite = s;
                        layers[i].transform.localPosition = offset;
                        layers[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        layers[i].gameObject.SetActive(false);
                        layers[i].sprite = null;
                    }
                }

                // Now we only have standings with 1:2 aspect ratio
                // TODO: set renderTexture's aspect ratio
                RenderTexture renderTexture;
                if (textureCache.Count == textureCache.MaxSize)
                {
                    renderTexture = textureCache.PopLeastUsed();
                }
                else
                {
                    renderTexture = new RenderTexture(referenceSize / 2, referenceSize, 0)
                    {
                        name = "CharacterMergerRenderTexture",
                    };
                }

                mergeCamera.orthographicSize = (float) referenceSize / pixelsPerUnit / 2;
                mergeCamera.RenderToTexture(renderTexture);

                for (int i = 0; i < sprites.Count; i++)
                {
                    layers[i].gameObject.SetActive(false);
                }

                result = textureCache[key] = renderTexture;
            }

            return result;
        }
    }
}