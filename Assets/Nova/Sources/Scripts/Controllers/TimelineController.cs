using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Nova
{
    [ExportCustomType]
    public class TimelineController : MonoBehaviour, IPrioritizedRestorable
    {
        public string luaName;
        public string timelinePrefabFolder;
        public Camera mainCamera;

        public string currentTimelinePrefabName { get; private set; }

        private GameState gameState;

        private GameObject timelinePrefab;
        private GameObject timeline;
        public PlayableDirector playableDirector { get; private set; }

        public PostProcessing cameraPP;

        private void Awake()
        {
            gameState = Utils.FindNovaGameController().GameState;
            cameraPP = mainCamera.GetComponent<PostProcessing>();

            if (!string.IsNullOrEmpty(luaName))
            {
                LuaRuntime.Instance.BindObject(luaName, this);
                gameState.AddRestorable(this);
            }
        }

        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(luaName))
            {
                gameState.RemoveRestorable(this);
            }
        }

        #region Methods called by external scripts

        public GameObject LoadTimelinePrefab(string timelinePrefabName)
        {
            if (timelinePrefabName == currentTimelinePrefabName)
            {
                return timelinePrefab;
            }

            return AssetLoader.Load<GameObject>(System.IO.Path.Combine(timelinePrefabFolder, timelinePrefabName));
        }

        public void SetTimelinePrefab(string timelinePrefabName)
        {
            if (timelinePrefabName == currentTimelinePrefabName)
            {
                return;
            }

            ClearTimelinePrefab();
            timelinePrefab = LoadTimelinePrefab(timelinePrefabName);
            timeline = Instantiate(timelinePrefab, transform);
            timeline.SetActive(false);

            playableDirector = timeline.GetComponent<PlayableDirector>();
            if (playableDirector != null)
            {
                playableDirector.timeUpdateMode = DirectorUpdateMode.Manual;
                playableDirector.playOnAwake = false;
                playableDirector.Evaluate();
            }

            Camera newCamera = timeline.GetComponentInChildren<Camera>();
            if (newCamera != null)
            {
                PostProcessing ppClient = newCamera.GetComponent<PostProcessing>();
                ppClient.asProxyOf = cameraPP;
                mainCamera.GetComponent<CameraController>().overridingCamera = newCamera;
                mainCamera.enabled = false;
                newCamera.targetTexture = mainCamera.targetTexture;
                this.RuntimeAssert(newCamera.GetComponent<CameraController>() == null,
                    "Timeline does not include another CameraController.");
                // Debug.Log("Switched main camera to timeline provided camera");
            }

            timeline.SetActive(true);
            currentTimelinePrefabName = timelinePrefabName;
        }

        // Use after animation entry of TimeAnimationProperty is destroyed
        public void ClearTimelinePrefab()
        {
            if (currentTimelinePrefabName == null)
            {
                return;
            }

            mainCamera.GetComponent<CameraController>().overridingCamera = null;
            mainCamera.enabled = true;
            // Debug.Log("Switched main camera back to original camera");
            timeline.SetActive(false);
            Destroy(timeline);
            timeline = null;
            playableDirector = null;
            currentTimelinePrefabName = null;
        }

        #endregion

        [Serializable]
        private class TimelineRestoreData : IRestoreData
        {
            public readonly string currentTimelinePrefabName;
            public readonly TransformRestoreData transformRestoreData;
            public readonly float time;

            public TimelineRestoreData(string currentTimelinePrefabName, Transform transform, float time)
            {
                this.currentTimelinePrefabName = currentTimelinePrefabName;
                transformRestoreData = new TransformRestoreData(transform);
                this.time = time;
            }
        }

        public string restorableObjectName => luaName;

        public RestorablePriority priority => RestorablePriority.Early;

        public IRestoreData GetRestoreData()
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

            return new TimelineRestoreData(currentTimelinePrefabName, transform, time);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as TimelineRestoreData;
            data.transformRestoreData.Restore(transform);
            if (data.currentTimelinePrefabName != null)
            {
                SetTimelinePrefab(data.currentTimelinePrefabName);
                if (playableDirector != null)
                {
                    playableDirector.time = data.time;
                    playableDirector.Evaluate();
                }
            }
            else
            {
                ClearTimelinePrefab();
            }
        }
    }
}