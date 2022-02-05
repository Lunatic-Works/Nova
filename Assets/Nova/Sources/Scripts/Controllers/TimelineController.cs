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

        private CameraController cameraController;

        protected override void Awake()
        {
            base.Awake();

            cameraController = mainCamera.GetComponent<CameraController>();
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

                cameraController.overridingCamera = newCamera;
                mainCamera.enabled = false;
                newCamera.targetTexture = mainCamera.targetTexture;
                // Debug.Log("Switched main camera to timeline provided camera");
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

            cameraController.overridingCamera = null;
            mainCamera.enabled = true;
            // Debug.Log("Switched main camera back to original camera");

            base.ClearPrefab();
        }

        #endregion

        [Serializable]
        private class TimelineRestoreData : PrefabRestoreData
        {
            public readonly float time;

            public TimelineRestoreData(PrefabRestoreData baseData, float time) : base(baseData)
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

            return new TimelineRestoreData(base.GetRestoreData() as PrefabRestoreData, time);
        }

        public override void Restore(IRestoreData restoreData)
        {
            var baseData = restoreData as PrefabRestoreData;
            base.Restore(baseData);

            var data = restoreData as TimelineRestoreData;
            if (playableDirector != null)
            {
                playableDirector.time = data.time;
                playableDirector.Evaluate();
            }
        }
    }
}