using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Nova
{
    [ExportCustomType]
    public class TimelineController : PrefabLoader
    {
        public Camera mainCamera;

        public PlayableDirector playableDirector { get; private set; }

        private CameraController mainCameraController;
        private PostProcessing mainPostProcessing;

        protected override void Awake()
        {
            base.Awake();

            mainCameraController = mainCamera.GetComponent<CameraController>();
            mainPostProcessing = mainCamera.GetComponent<PostProcessing>();
        }

        #region Methods called by external scripts

        public override void SetPrefab(string prefabName)
        {
            if (prefabName == currentPrefabName)
            {
                return;
            }

            base.SetPrefab(prefabName);

            prefabInstance.SetActive(false);

            playableDirector = prefabInstance.GetComponent<PlayableDirector>();
            if (playableDirector != null)
            {
                playableDirector.timeUpdateMode = DirectorUpdateMode.Manual;
                playableDirector.playOnAwake = false;
                playableDirector.Evaluate();
            }

            Camera newCamera = prefabInstance.GetComponentInChildren<Camera>();
            if (newCamera != null)
            {
                this.RuntimeAssert(newCamera.GetComponent<CameraController>() == null,
                    "The camera in the timeline prefab should not have a CameraController.");

                mainCameraController.overridingCamera = newCamera;
                mainCamera.enabled = false;
                newCamera.targetTexture = mainCamera.targetTexture;

                var newPostProcessing = newCamera.GetComponent<PostProcessing>();
                if (newPostProcessing == null)
                {
                    Debug.LogWarning("Nova: No PostProcessing on new camera.");
                }
                else
                {
                    newPostProcessing.asProxyOf = mainPostProcessing;
                }
            }

            prefabInstance.SetActive(true);
        }

        // Use after all animation entries of TimeAnimationProperty are terminated
        public override void ClearPrefab()
        {
            if (string.IsNullOrEmpty(currentPrefabName))
            {
                return;
            }

            playableDirector = null;

            mainCameraController.overridingCamera = null;
            mainCamera.enabled = true;

            base.ClearPrefab();
        }

        #endregion

        [Serializable]
        private class TimelineControllerRestoreData : PrefabLoaderRestoreData
        {
            public readonly float time;

            public TimelineControllerRestoreData(PrefabLoaderRestoreData baseData, float time) : base(baseData)
            {
                this.time = time;
            }
        }

        public override IRestoreData GetRestoreData()
        {
            float time;
            if (playableDirector != null)
            {
                time = (float)playableDirector.time;
            }
            else
            {
                time = 0.0f;
            }

            return new TimelineControllerRestoreData(base.GetRestoreData() as PrefabLoaderRestoreData, time);
        }

        public override void Restore(IRestoreData restoreData)
        {
            var baseData = restoreData as PrefabLoaderRestoreData;
            base.Restore(baseData);

            var data = restoreData as TimelineControllerRestoreData;
            if (playableDirector != null)
            {
                playableDirector.time = data.time;
                playableDirector.Evaluate();
            }
        }
    }
}
