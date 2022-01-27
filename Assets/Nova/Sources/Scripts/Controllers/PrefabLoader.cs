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
            gameState = Utils.FindNovaGameController().GameState;

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

        [Serializable]
        protected class PrefabRestoreData : IRestoreData
        {
            public readonly string currentPrefabName;
            public readonly TransformRestoreData transformRestoreData;

            public PrefabRestoreData(string currentPrefabName, Transform transform)
            {
                this.currentPrefabName = currentPrefabName;
                transformRestoreData = new TransformRestoreData(transform);
            }

            public PrefabRestoreData(PrefabRestoreData baseData)
            {
                currentPrefabName = baseData.currentPrefabName;
                transformRestoreData = baseData.transformRestoreData;
            }
        }

        public string restorableObjectName => luaName;

        public RestorablePriority priority => RestorablePriority.Early;

        public virtual IRestoreData GetRestoreData()
        {
            return new PrefabRestoreData(currentPrefabName, transform);
        }

        public virtual void Restore(IRestoreData restoreData)
        {
            var data = restoreData as PrefabRestoreData;
            data.transformRestoreData.Restore(transform);
            if (!string.IsNullOrEmpty(data.currentPrefabName))
            {
                SetPrefab(data.currentPrefabName);
            }
            else
            {
                ClearPrefab();
            }
        }
    }
}