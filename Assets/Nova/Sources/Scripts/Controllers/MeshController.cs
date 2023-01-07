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
            gameState = Utils.FindNovaController().GameState;
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

        #region Restoration

        public string restorableName => luaGlobalName;

        [Serializable]
        private class MeshControllerRestoreData : IRestoreData
        {
            public readonly bool meshEnabled;

            public MeshControllerRestoreData(bool meshEnabled)
            {
                this.meshEnabled = meshEnabled;
            }
        }

        public IRestoreData GetRestoreData()
        {
            return new MeshControllerRestoreData(meshEnabled);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as MeshControllerRestoreData;
            meshEnabled = data.meshEnabled;
        }

        #endregion
    }
}
