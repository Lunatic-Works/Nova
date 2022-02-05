using System;
using UnityEngine;

namespace Nova
{
    // a lazy proxy to deal with timeline camera switching
    [ExportCustomType]
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour, IRestorable
    {
        public string luaGlobalName;
        public Camera overridingCamera;

        private GameState gameState;
        private new Camera camera;

        private Camera activeCamera
        {
            get
            {
                if (overridingCamera != null)
                {
                    return overridingCamera;
                }

                return camera;
            }
        }

        #region Proxied methods and properties

        public new Transform transform => activeCamera.transform;
        public new GameObject gameObject => activeCamera.gameObject;
        public GameObject baseGameObject => base.gameObject;

        public new Component GetComponent(Type t)
        {
            return activeCamera.GetComponent(t);
        }

        public new T GetComponent<T>()
        {
            return activeCamera.GetComponent<T>();
        }

        public int cullingMask
        {
            get => activeCamera.cullingMask;
            set => activeCamera.cullingMask = value;
        }

        #endregion

        public float size
        {
            get
            {
                if (activeCamera.orthographic)
                {
                    return activeCamera.orthographicSize;
                }
                else
                {
                    return activeCamera.fieldOfView;
                }
            }
            set
            {
                if (activeCamera.orthographic)
                {
                    activeCamera.orthographicSize = value;
                }
                else
                {
                    activeCamera.fieldOfView = value;
                }
            }
        }

        private void Awake()
        {
            gameState = Utils.FindNovaGameController().GameState;
            camera = base.GetComponent<Camera>();

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
        private class CameraRestoreData : IRestoreData
        {
            public readonly TransformRestoreData transformRestoreData;
            public readonly float size;
            public readonly int cullingMask;

            public CameraRestoreData(Transform transform, float size, int cullingMask)
            {
                transformRestoreData = new TransformRestoreData(transform);
                this.size = size;
                this.cullingMask = cullingMask;
            }
        }

        [Serializable]
        private class CameraControllerRestoreData : IRestoreData
        {
            public readonly CameraRestoreData cameraRestoreData;
            public readonly CameraRestoreData overridingCameraRestoreData;

            public CameraControllerRestoreData(CameraController cameraController)
            {
                var currentOverridingCamera = cameraController.overridingCamera;
                cameraController.overridingCamera = null;
                cameraRestoreData = new CameraRestoreData(cameraController.transform, cameraController.size,
                    cameraController.cullingMask);
                cameraController.overridingCamera = currentOverridingCamera;
                overridingCameraRestoreData = cameraController.overridingCamera != null
                    ? new CameraRestoreData(cameraController.transform, cameraController.size,
                        cameraController.cullingMask)
                    : null;
            }
        }

        public string restorableObjectName => luaGlobalName;

        public IRestoreData GetRestoreData()
        {
            return new CameraControllerRestoreData(this);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as CameraControllerRestoreData;
            var currentOverridingCamera = overridingCamera;
            overridingCamera = null;
            data.cameraRestoreData.transformRestoreData.Restore(transform);
            size = data.cameraRestoreData.size;
            cullingMask = data.cameraRestoreData.cullingMask;
            overridingCamera = currentOverridingCamera;

            if (currentOverridingCamera != null && data.overridingCameraRestoreData != null)
            {
                data.overridingCameraRestoreData.transformRestoreData.Restore(transform);
                size = data.overridingCameraRestoreData.size;
                cullingMask = data.overridingCameraRestoreData.cullingMask;
            }
        }
    }
}