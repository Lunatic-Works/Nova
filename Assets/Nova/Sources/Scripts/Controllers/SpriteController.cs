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

        // An extra transform to resize the sprite according to SpriteWithOffset
        // Set resize = this.transform to disable
        // No need to restore resizer transform
        public Transform resizer;

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
            get => resizer.gameObject.layer;
            set => resizer.gameObject.layer = value;
        }

        public int sortingOrder
        {
            get
            {
                if (spriteRenderer != null)
                {
                    return spriteRenderer.sortingOrder;
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.sortingOrder = value;
                }
                else
                {
                    Debug.LogWarning($"Nova: Cannot set sortingOrder for Image: {Utils.GetPath(this)}");
                }
            }
        }

        private void Awake()
        {
            gameState = Utils.FindNovaController().GameState;
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            image = GetComponentInChildren<Image>();
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

        public void SetImage(string imageName)
        {
            if (imageName == currentImageName)
            {
                return;
            }

            var imagePath = System.IO.Path.Combine(imageFolder, imageName);
            if (resizer == transform)
            {
                sprite = AssetLoader.Load<Sprite>(imagePath);
            }
            else
            {
                var spriteWithOffset = AssetLoader.LoadOrNull<SpriteWithOffset>(imagePath);
                if (spriteWithOffset != null)
                {
                    sprite = spriteWithOffset.sprite;
                    resizer.localPosition = spriteWithOffset.offset;
                    resizer.localScale = spriteWithOffset.scale;
                }
                else
                {
                    sprite = AssetLoader.Load<Sprite>(imagePath);
                    resizer.localPosition = Vector3.zero;
                    resizer.localScale = Vector3.one;
                }
            }

            currentImageName = imageName;
        }

        public void ClearImage()
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
            public readonly int layer;
            public readonly int sortingOrder;

            public SpriteControllerRestoreData(SpriteController parent)
            {
                currentImageName = parent.currentImageName;
                transformData = new TransformData(parent.transform);
                color = parent.color;

                // Material must be RestorableMaterial or defaultMaterial
                if (parent.sharedMaterial is RestorableMaterial)
                {
                    materialData = RestorableMaterial.GetRestoreData(parent.sharedMaterial);
                }
                else
                {
                    materialData = null;
                }

                layer = parent.layer;
                sortingOrder = parent.sortingOrder;
            }
        }

        public IRestoreData GetRestoreData()
        {
            return new SpriteControllerRestoreData(this);
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

            layer = data.layer;
            if (spriteRenderer != null)
            {
                sortingOrder = data.sortingOrder;
            }

            if (data.currentImageName == currentImageName)
            {
                return;
            }

            if (!string.IsNullOrEmpty(data.currentImageName))
            {
                SetImage(data.currentImageName);
            }
            else
            {
                ClearImage();
            }
        }

        #endregion
    }
}
