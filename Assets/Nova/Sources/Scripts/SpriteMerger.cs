using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    public class SpriteMerger : MonoBehaviour
    {
        public int layer = 16;
        public Vector2Int referenceSize = new Vector2Int(2048, 4096);
        public float pixelsPerUnit = 100.0f;

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
            if (textureCache.ContainsKey(key))
            {
                return textureCache[key];
            }

            EnsureLayers(sprites.Count);
            for (var i = 0; i < layers.Count; i++)
            {
                if (i < sprites.Count)
                {
                    layers[i].sprite = sprites[i].sprite;
                    layers[i].transform.localPosition = sprites[i].offset;
                    layers[i].gameObject.SetActive(true);
                }
                else
                {
                    layers[i].gameObject.SetActive(false);
                    layers[i].sprite = null;
                }
            }

            RenderTexture texture;
            if (textureCache.Count == textureCache.MaxSize)
            {
                texture = textureCache.PopLeastUsed();
            }
            else
            {
                texture = new RenderTexture(referenceSize.x, referenceSize.y, 0)
                {
                    name = "SpriteMergerRenderTexture"
                };
            }

            mergeCamera.orthographicSize = (float)Mathf.Max(referenceSize.x, referenceSize.y) / pixelsPerUnit / 2;
            mergeCamera.RenderToTexture(texture);
            textureCache[key] = texture;

            for (int i = 0; i < sprites.Count; i++)
            {
                layers[i].gameObject.SetActive(false);
                layers[i].sprite = null;
            }

            return texture;
        }

        public Texture GetMergedTexture(List<Sprite> sprites)
        {
            return GetMergedTexture(sprites.Select(sprite =>
                {
                    var so = ScriptableObject.CreateInstance<SpriteWithOffset>();
                    so.sprite = sprite;
                    so.offset = Vector3.zero;
                    return so;
                })
                .ToList());
        }

        public void ClearCache()
        {
            textureCache.Clear();
        }
    }
}