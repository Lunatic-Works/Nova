using System;
using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour, IRestorable
    {
        public string luaGlobalName;

        private GameState gameState;
        public new Camera camera { get; private set; }

        public bool cameraEnabled
        {
            get => camera.enabled;
            set => camera.enabled = value;
        }

        public float size
        {
            get
            {
                if (camera.orthographic)
                {
                    return camera.orthographicSize;
                }
                else
                {
                    return camera.fieldOfView;
                }
            }
            set
            {
                if (camera.orthographic)
                {
                    camera.orthographicSize = value;
                }
                else
                {
                    camera.fieldOfView = value;
                }
            }
        }

        public int cullingMask
        {
            get => camera.cullingMask;
            set => camera.cullingMask = value;
        }

        private void Awake()
        {
            gameState = Utils.FindNovaController().GameState;
            camera = GetComponent<Camera>();

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
        private class CameraControllerRestoreData : IRestoreData
        {
            public readonly TransformData transformData;
            public readonly bool cameraEnabled;
            public readonly float size;
            public readonly int cullingMask;

            public CameraControllerRestoreData(CameraController parent)
            {
                transformData = new TransformData(parent.transform);
                cameraEnabled = parent.cameraEnabled;
                size = parent.size;
                cullingMask = parent.cullingMask;
            }
        }

        public IRestoreData GetRestoreData()
        {
            return new CameraControllerRestoreData(this);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as CameraControllerRestoreData;
            data.transformData.Restore(transform);
            cameraEnabled = data.cameraEnabled;
            size = data.size;
            cullingMask = data.cullingMask;
        }

        #endregion
    }
}
