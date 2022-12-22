using System;
using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class PrefabLoader : MonoBehaviour, IPrioritizedRestorable
    {
        public string luaName;
        public string prefabFolder;

        public string currentPrefabName { get; private set; }

        protected GameState gameState;

        protected GameObject prefabInstance;

        protected virtual void Awake()
        {
            gameState = Utils.FindNovaController().GameState;

            if (!string.IsNullOrEmpty(luaName))
            {
                LuaRuntime.Instance.BindObject(luaName, this);
                gameState.AddRestorable(this);
            }
        }

        protected virtual void OnDestroy()
        {
            if (!string.IsNullOrEmpty(luaName))
            {
                gameState.RemoveRestorable(this);
            }
        }

        #region Methods called by external scripts

        public virtual void SetPrefab(string prefabName)
        {
            if (prefabName == currentPrefabName)
            {
                return;
            }

            ClearPrefab();

            var prefab = AssetLoader.Load<GameObject>(System.IO.Path.Combine(prefabFolder, prefabName));
            prefabInstance = Instantiate(prefab, transform);

            currentPrefabName = prefabName;
        }

        public virtual void ClearPrefab()
        {
            if (string.IsNullOrEmpty(currentPrefabName))
            {
                return;
            }

            prefabInstance.SetActive(false);
            Destroy(prefabInstance);
            prefabInstance = null;

            currentPrefabName = null;
        }

        #endregion

        #region Restoration

        public string restorableName => luaName;
        public RestorablePriority priority => RestorablePriority.Early;

        [Serializable]
        protected class PrefabLoaderRestoreData : IRestoreData
        {
            public readonly string currentPrefabName;
            public readonly TransformData transformData;

            public PrefabLoaderRestoreData(string currentPrefabName, Transform transform)
            {
                this.currentPrefabName = currentPrefabName;
                transformData = new TransformData(transform);
            }

            public PrefabLoaderRestoreData(PrefabLoaderRestoreData baseData)
            {
                currentPrefabName = baseData.currentPrefabName;
                transformData = baseData.transformData;
            }
        }

        public virtual IRestoreData GetRestoreData()
        {
            return new PrefabLoaderRestoreData(currentPrefabName, transform);
        }

        public virtual void Restore(IRestoreData restoreData)
        {
            var data = restoreData as PrefabLoaderRestoreData;
            data.transformData.Restore(transform);
            if (!string.IsNullOrEmpty(data.currentPrefabName))
            {
                SetPrefab(data.currentPrefabName);
            }
            else
            {
                ClearPrefab();
            }
        }

        #endregion
    }
}
