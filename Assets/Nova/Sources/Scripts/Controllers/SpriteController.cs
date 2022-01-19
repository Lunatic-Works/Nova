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
        private DialogueBoxController dialogueBoxController;

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
            gameState = Utils.FindNovaGameController().GameState;
            spriteRenderer = GetComponent<SpriteRenderer>();
            image = GetComponent<Image>();
            if (image != null && image.sprite == null)
            {
                // Using an empty png is not working due to unknown reason..
                image.sprite = Utils.Texture2DToSprite(Utils.ClearTexture);
            }

            this.RuntimeAssert(spriteRenderer != null || image != null, "Missing SpriteRenderer or Image.");
            defaultSprite = sprite;
            dialogueBoxController = Utils.FindViewManager().GetController<DialogueBoxController>();

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

        [Serializable]
        private class SpriteRestoreData : IRestoreData
        {
            public readonly string currentImageName;
            public readonly TransformRestoreData transformRestoreData;
            public readonly Vector4Data color;
            public readonly MaterialRestoreData materialRestoreData;
            public readonly int renderQueue;
            public readonly int layer;

            public SpriteRestoreData(string currentImageName, Transform transform, Color color,
                MaterialRestoreData materialRestoreData, int renderQueue, int layer)
            {
                this.currentImageName = currentImageName;
                transformRestoreData = new TransformRestoreData(transform);
                this.color = color;
                this.materialRestoreData = materialRestoreData;
                this.renderQueue = renderQueue;
                this.layer = layer;
            }
        }

        public string restorableObjectName => luaGlobalName;

        public IRestoreData GetRestoreData()
        {
            // Material must be RestorableMaterial or defaultMaterial
            MaterialRestoreData materialRestoreData;
            if (sharedMaterial is RestorableMaterial)
            {
                materialRestoreData = RestorableMaterial.GetRestoreData(sharedMaterial);
            }
            else
            {
                materialRestoreData = null;
            }

            int renderQueue = RenderQueueOverrider.Ensure(gameObject).renderQueue;

            return new SpriteRestoreData(currentImageName, transform, color, materialRestoreData, renderQueue, layer);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as SpriteRestoreData;
            data.transformRestoreData.Restore(transform);
            color = data.color;

            // Material must be RestorableMaterial or defaultMaterial
            if (data.materialRestoreData != null)
            {
                MaterialFactory factory = MaterialPool.Ensure(gameObject).factory;
                material = RestorableMaterial.RestoreMaterialFromData(data.materialRestoreData, factory);
            }
            else
            {
                material = MaterialPool.Ensure(gameObject).defaultMaterial;
            }

            RenderQueueOverrider.Ensure(gameObject).renderQueue = data.renderQueue;
            layer = data.layer;

            if (data.currentImageName == currentImageName)
            {
                return;
            }

            if (data.currentImageName != null)
            {
                SetImage(data.currentImageName, fade: false);
            }
            else
            {
                ClearImage(fade: false);
            }
        }
    }
}