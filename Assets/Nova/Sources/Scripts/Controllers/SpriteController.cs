using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [ExportCustomType]
    public class SpriteController : MonoBehaviour, IRestorable
    {
        public string luaGlobalName;
        public string imageFolder;

        public string currentImageName { get; private set; }

        private GameState gameState;
        private SpriteRenderer spriteRenderer;
        private Image image;
        private Sprite defaultSprite;

        public Sprite sprite
        {
            get
            {
                if (spriteRenderer != null)
                {
                    return spriteRenderer.sprite;
                }
                else
                {
                    return image.sprite;
                }
            }
            set
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = value;
                }
                else
                {
                    if (value == null)
                    {
                        value = defaultSprite;
                    }

                    image.sprite = value;
                    image.SetNativeSize();
                }
            }
        }

        public Color color
        {
            get
            {
                if (spriteRenderer != null)
                {
                    return spriteRenderer.color;
                }
                else
                {
                    return image.color;
                }
            }
            set
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = value;
                }
                else
                {
                    image.color = value;
                }
            }
        }

        public Material material
        {
            set
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.material = value;
                }
                else
                {
                    image.material = value;
                }
            }
        }

        public Material sharedMaterial
        {
            get
            {
                if (spriteRenderer != null)
                {
                    return spriteRenderer.sharedMaterial;
                }
                else
                {
                    return image.material;
                }
            }
        }

        public int layer
        {
            get => gameObject.layer;
            set => gameObject.layer = value;
        }

        private void Awake()
        {
            gameState = Utils.FindNovaController().GameState;
            spriteRenderer = GetComponent<SpriteRenderer>();
            image = GetComponent<Image>();
            if (image != null && image.sprite == null)
            {
                // Using an empty png is not working due to unknown reason..
                image.sprite = Utils.Texture2DToSprite(Utils.ClearTexture);
            }

            this.RuntimeAssert(spriteRenderer != null || image != null, "Missing SpriteRenderer or Image.");
            defaultSprite = sprite;

            if (!string.IsNullOrEmpty(luaGlobalName))
            {
                LuaRuntime.Instance.BindObject(luaGlobalName, this, "_G");
                gameState.AddRestorable(this);
            }

            I18n.LocaleChanged.AddListener(OnLocaleChanged);
        }

        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(luaGlobalName))
            {
                gameState.RemoveRestorable(this);
            }

            I18n.LocaleChanged.RemoveListener(OnLocaleChanged);
        }

        #region Methods called by external scripts

        public void SetImage(string imageName, bool fade = true)
        {
            if (imageName == currentImageName)
            {
                return;
            }

            Sprite newSprite = AssetLoader.Load<Sprite>(System.IO.Path.Combine(imageFolder, imageName));
            sprite = newSprite;
            currentImageName = imageName;
        }

        public void ClearImage(bool fade = true)
        {
            if (string.IsNullOrEmpty(currentImageName))
            {
                return;
            }

            sprite = defaultSprite;
            currentImageName = null;
        }

        #endregion

        // Load localized image on locale changed
        private void OnLocaleChanged()
        {
            if (string.IsNullOrEmpty(currentImageName))
            {
                return;
            }

            sprite = AssetLoader.Load<Sprite>(System.IO.Path.Combine(imageFolder, currentImageName));
        }

        #region Restoration

        public string restorableName => luaGlobalName;

        [Serializable]
        private class SpriteControllerRestoreData : IRestoreData
        {
            public readonly string currentImageName;
            public readonly TransformData transformData;
            public readonly Vector4Data color;
            public readonly MaterialData materialData;
            public readonly int renderQueue;
            public readonly int layer;

            public SpriteControllerRestoreData(string currentImageName, Transform transform, Color color,
                MaterialData materialData, int renderQueue, int layer)
            {
                this.currentImageName = currentImageName;
                transformData = new TransformData(transform);
                this.color = color;
                this.materialData = materialData;
                this.renderQueue = renderQueue;
                this.layer = layer;
            }
        }

        public IRestoreData GetRestoreData()
        {
            // Material must be RestorableMaterial or defaultMaterial
            MaterialData materialData;
            if (sharedMaterial is RestorableMaterial)
            {
                materialData = RestorableMaterial.GetRestoreData(sharedMaterial);
            }
            else
            {
                materialData = null;
            }

            int renderQueue = gameObject.Ensure<RenderQueueOverrider>().renderQueue;

            return new SpriteControllerRestoreData(currentImageName, transform, color, materialData, renderQueue, layer);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as SpriteControllerRestoreData;
            data.transformData.Restore(transform);
            color = data.color;

            // Material must be RestorableMaterial or defaultMaterial
            if (data.materialData != null)
            {
                MaterialFactory factory = gameObject.Ensure<MaterialPool>().factory;
                material = RestorableMaterial.Restore(data.materialData, factory);
            }
            else
            {
                material = gameObject.Ensure<MaterialPool>().defaultMaterial;
            }

            gameObject.Ensure<RenderQueueOverrider>().renderQueue = data.renderQueue;
            layer = data.layer;

            if (data.currentImageName == currentImageName)
            {
                return;
            }

            if (!string.IsNullOrEmpty(data.currentImageName))
            {
                SetImage(data.currentImageName, fade: false);
            }
            else
            {
                ClearImage(fade: false);
            }
        }

        #endregion
    }
}
