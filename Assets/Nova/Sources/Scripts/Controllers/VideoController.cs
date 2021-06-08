using System;
using UnityEngine;
using UnityEngine.Video;

namespace Nova
{
    [ExportCustomType]
    [RequireComponent(typeof(VideoPlayer))]
    public class VideoController : MonoBehaviour, IRestorable
    {
        public string luaName;
        public string videoFolder;

        public string currentVideoName { get; private set; }

        private GameState gameState;

        public VideoPlayer videoPlayer { get; private set; }

        private void Awake()
        {
            gameState = Utils.FindNovaGameController().GameState;
            videoPlayer = GetComponent<VideoPlayer>();

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

        public void SetVideo(string videoName)
        {
            if (videoName == currentVideoName)
            {
                return;
            }

            videoPlayer.clip = AssetLoader.Load<VideoClip>(System.IO.Path.Combine(videoFolder, videoName));
            // TODO: how to preload video?
            videoPlayer.Prepare();
            currentVideoName = videoName;
        }

        // Use after animation entry of TimeAnimationProperty is destroyed
        public void ClearVideo()
        {
            if (currentVideoName == null)
            {
                return;
            }

            videoPlayer.clip = null;
            currentVideoName = null;
        }

        #endregion

        [Serializable]
        private class VideoRestoreData : IRestoreData
        {
            public readonly string currentVideoName;

            public VideoRestoreData(string currentVideoName)
            {
                this.currentVideoName = currentVideoName;
            }
        }

        public string restorableObjectName => luaName;

        public IRestoreData GetRestoreData()
        {
            return new VideoRestoreData(currentVideoName);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as VideoRestoreData;
            if (data.currentVideoName != null)
            {
                SetVideo(data.currentVideoName);
            }
            else
            {
                ClearVideo();
            }
        }
    }
}