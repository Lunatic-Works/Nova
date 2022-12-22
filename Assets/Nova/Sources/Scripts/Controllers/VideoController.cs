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
            gameState = Utils.FindNovaController().GameState;
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
            if (string.IsNullOrEmpty(currentVideoName))
            {
                return;
            }

            videoPlayer.clip = null;
            currentVideoName = null;
        }

        #endregion

        #region Restoration

        public string restorableName => luaName;

        [Serializable]
        private class VideoControllerRestoreData : IRestoreData
        {
            public readonly string currentVideoName;

            public VideoControllerRestoreData(string currentVideoName)
            {
                this.currentVideoName = currentVideoName;
            }
        }

        public IRestoreData GetRestoreData()
        {
            return new VideoControllerRestoreData(currentVideoName);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as VideoControllerRestoreData;
            if (!string.IsNullOrEmpty(data.currentVideoName))
            {
                SetVideo(data.currentVideoName);
            }
            else
            {
                ClearVideo();
            }
        }

        #endregion
    }
}
