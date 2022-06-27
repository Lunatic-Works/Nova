using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [RequireComponent(typeof(RawImage))]
    [ExportCustomType]
    public class RawImageController : MonoBehaviour, IRestorable
    {
        public string luaGlobalName;

        private GameState gameState;
        private RawImage image;

        public Material material
        {
            set => image.material = value;
        }

        public Material sharedMaterial => image.material;

        private void Awake()
        {
            gameState = Utils.FindNovaGameController().GameState;
            image = GetComponent<RawImage>();

            if (!string.IsNullOrEmpty(luaGlobalName))
            {
                LuaRuntime.Instance.BindObject(luaGlobalName, this, "_G");
                gameState.AddRestorable(this);
            }
        }

        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(luaGlobalName))
            {
                gameState.RemoveRestorable(this);
            }
        }

        #region Restoration

        public string restorableName => luaGlobalName;

        [Serializable]
        private class SpriteRestoreData : IRestoreData
        {
            public readonly MaterialData materialData;

            public SpriteRestoreData(MaterialData materialData)
            {
                this.materialData = materialData;
            }
        }

        public IRestoreData GetRestoreData()
        {
            // Material must be RestorableMaterial or DefaultMaterial
            MaterialData materialData;
            if (sharedMaterial is RestorableMaterial)
            {
                materialData = RestorableMaterial.GetRestoreData(sharedMaterial);
            }
            else
            {
                materialData = null;
            }

            return new SpriteRestoreData(materialData);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as SpriteRestoreData;

            // Material must be RestorableMaterial or DefaultMaterial
            if (data.materialData != null)
            {
                MaterialFactory factory = gameObject.Ensure<MaterialPool>().factory;
                material = RestorableMaterial.Restore(data.materialData, factory);
            }
            else
            {
                material = gameObject.Ensure<MaterialPool>().defaultMaterial;
            }
        }

        #endregion
    }
}