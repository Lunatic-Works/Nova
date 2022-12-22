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
        private new Camera camera;

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
            camera = GetComponent<Camera>();
            gameState = Utils.FindNovaController().GameState;

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

        [Serializable]
        private class CameraData
        {
            public readonly TransformData transformData;
            public readonly float size;
            public readonly int cullingMask;

            public CameraData(Transform transform, float size, int cullingMask)
            {
                transformData = new TransformData(transform);
                this.size = size;
                this.cullingMask = cullingMask;
            }
        }

        #region Restoration

        public string restorableName => luaGlobalName;

        [Serializable]
        private class CameraControllerRestoreData : IRestoreData
        {
            public readonly CameraData cameraData;

            public CameraControllerRestoreData(CameraController cameraController)
            {
                cameraData = new CameraData(cameraController.transform, cameraController.size,
                    cameraController.cullingMask);
            }
        }

        public IRestoreData GetRestoreData()
        {
            return new CameraControllerRestoreData(this);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as CameraControllerRestoreData;
            data.cameraData.transformData.Restore(transform);
            size = data.cameraData.size;
            cullingMask = data.cameraData.cullingMask;
        }

        #endregion
    }
}
