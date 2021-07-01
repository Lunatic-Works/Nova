using System;
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

        private readonly Dictionary<string, LRUCache<string, RenderTexture>> textureCaches =
            new Dictionary<string, LRUCache<string, RenderTexture>>();

        private readonly HashSet<string> cachesInUse = new HashSet<string>();
        private readonly List<SpriteRenderer> layers = new List<SpriteRenderer>();

        private Texture empty;
        private Camera mergeCamera;

        private void Awake()
        {
            empty = Utils.ClearTexture;

            mergeCamera = gameObject.AddComponent<Camera>();
            mergeCamera.orthographic = true;
            mergeCamera.enabled = false;
            mergeCamera.cullingMask = 1 << layer;
            mergeCamera.nearClipPlane = -1;
            mergeCamera.farClipPlane = 1;

            Application.lowMemory += OnLowMemory;
        }

        private void OnDestroy()
        {
            foreach (var tc in textureCaches.Values)
            {
                tc.Clear();
            }

            foreach (var sr in layers)
            {
                Utils.DestroyObject(sr.gameObject);
            }

            Utils.DestroyObject(empty);

            Application.lowMemory -= OnLowMemory;
        }

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

        public Texture GetMergedTexture(string cacheName, IReadOnlyList<SpriteWithOffset> sprites)
        {
            if (sprites.Count == 0)
            {
                return empty;
            }

            LRUCache<string, RenderTexture> cache;
            if (textureCaches.ContainsKey(cacheName))
            {
                cache = textureCaches[cacheName];
            }
            else
            {
                textureCaches[cacheName] = cache = new LRUCache<string, RenderTexture>(autoDestroy: true, maxSize: 2);
            }

            cachesInUse.Add(cacheName);

            var key = sprites.Aggregate("", (r, s) => r + s.GetInstanceID() + ":");
            if (cache.ContainsKey(key))
            {
                return cache[key];
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
            if (cache.Count == cache.MaxSize)
            {
                texture = cache.PopLeastUsed();
            }
            else
            {
                texture = new RenderTexture(referenceSize.x, referenceSize.y, 0)
                {
                    name = "SpriteMergerRenderTexture"
                };
            }

            mergeCamera.orthographicSize = (float)Math.Max(referenceSize.x, referenceSize.y) / pixelsPerUnit / 2;
            mergeCamera.RenderToTexture(texture);
            cache[key] = texture;

            for (int i = 0; i < sprites.Count; i++)
            {
                layers[i].gameObject.SetActive(false);
            }

            return texture;
        }

        public Texture GetMergedTexture(string cacheName, IEnumerable<Sprite> sprites)
        {
            return GetMergedTexture(cacheName, sprites.Select(sprite =>
                {
                    var so = ScriptableObject.CreateInstance<SpriteWithOffset>();
                    so.sprite = sprite;
                    so.offset = Vector3.zero;
                    return so;
                })
                .ToList());
        }

        public void ReleaseCache(string cacheName)
        {
            cachesInUse.Remove(cacheName);
        }

        private void OnLowMemory()
        {
            foreach (var cacheName in textureCaches.Keys)
            {
                var cache = textureCaches[cacheName];
                if (cachesInUse.Contains(cacheName))
                {
                    while (cache.Count > 1)
                    {
                        cache.PopLeastUsed();
                    }
                }
                else
                {
                    while (cache.Count > 0)
                    {
                        cache.PopLeastUsed();
                    }
                }
            }
        }
    }
}