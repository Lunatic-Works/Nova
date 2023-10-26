using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Nova
{
    [ExportCustomType]
    public class TimelineController : PrefabLoader
    {
        [SerializeField] private CameraController gameCamera;

        public PlayableDirector playableDirector { get; private set; }

        #region Methods called by external scripts

        public override void SetPrefab(string prefabName)
        {
            if (prefabName == currentPrefabName)
            {
                return;
            }

            base.SetPrefab(prefabName);

            prefabInstance.SetActive(false);

            if (prefabInstance.TryGetComponent<PlayableDirector>(out var _playableDirector))
            {
                playableDirector = _playableDirector;
                playableDirector.timeUpdateMode = DirectorUpdateMode.Manual;
                playableDirector.playOnAwake = false;
                playableDirector.Evaluate();
            }

            Camera newCamera = prefabInstance.GetComponentInChildren<Camera>();
            if (newCamera != null)
            {
                this.RuntimeAssert(!newCamera.TryGetComponent<CameraController>(out _),
                    "The camera in the timeline prefab should not have a CameraController.");

                gameCamera.cameraEnabled = false;
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

            gameCamera.cameraEnabled = true;

            base.ClearPrefab();
        }

        #endregion

        [Serializable]
        private class TimelineControllerRestoreData : PrefabLoaderRestoreData
        {
            public readonly float time;

            public TimelineControllerRestoreData(TimelineController parent) : base(parent)
            {
                if (parent.playableDirector != null)
                {
                    time = (float)parent.playableDirector.time;
                }
                else
                {
                    time = 0.0f;
                }
            }
        }

        public override IRestoreData GetRestoreData()
        {
            return new TimelineControllerRestoreData(this);
        }

        public override void Restore(IRestoreData restoreData)
        {
            base.Restore(restoreData);

            var data = restoreData as TimelineControllerRestoreData;
            if (playableDirector != null)
            {
                playableDirector.time = data.time;
                playableDirector.Evaluate();
            }
        }
    }
}
