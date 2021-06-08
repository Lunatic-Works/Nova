using System;
using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    [RequireComponent(typeof(MeshRenderer))]
    public class MeshController : MonoBehaviour, IRestorable
    {
        public string luaGlobalName;

        private GameState gameState;
        private MeshRenderer meshRenderer;

        private void Awake()
        {
            gameState = Utils.FindNovaGameController().GameState;
            meshRenderer = GetComponent<MeshRenderer>();
            meshEnabled = false;

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

        public bool meshEnabled
        {
            get => meshRenderer.enabled;
            set => meshRenderer.enabled = value;
        }

        [Serializable]
        private class MeshRestoreData : IRestoreData
        {
            public readonly bool meshEnabled;

            public MeshRestoreData(bool meshEnabled)
            {
                this.meshEnabled = meshEnabled;
            }
        }

        public string restorableObjectName => luaGlobalName;

        public IRestoreData GetRestoreData()
        {
            return new MeshRestoreData(meshEnabled);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as MeshRestoreData;
            meshEnabled = data.meshEnabled;
        }
    }
}