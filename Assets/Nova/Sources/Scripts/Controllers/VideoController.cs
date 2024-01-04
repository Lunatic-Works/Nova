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
        public float volume;

        public string currentVideoName { get; private set; }

        private GameState gameState;
        private VideoPlayer videoPlayer;

        public double duration => videoPlayer.clip.length;
        public bool isPlaying => videoPlayer.isPlaying;

        private void Awake()
        {
            gameState = Utils.FindNovaController().GameState;
            videoPlayer = GetComponent<VideoPlayer>();
            videoPlayer.errorReceived += OnError;

            if (!string.IsNullOrEmpty(luaName))
            {
                LuaRuntime.Instance.BindObject(luaName, this);
                gameState.AddRestorable(this);
            }
        }

        private void OnDestroy()
        {
            videoPlayer.errorReceived -= OnError;

            if (!string.IsNullOrEmpty(luaName))
            {
                gameState.RemoveRestorable(this);
            }
        }

        private void OnError(VideoPlayer player, string message)
        {
            Debug.LogWarning(message);
        }

        #region Methods called by external scripts

        public void SetVideo(string videoName)
        {
            videoPlayer.Stop();
            if (videoName == currentVideoName)
            {
                return;
            }

            videoPlayer.clip = AssetLoader.Load<VideoClip>(System.IO.Path.Combine(videoFolder, videoName));
            // TODO: how to preload video?
            videoPlayer.Prepare();
            currentVideoName = videoName;
        }

        public void ClearVideo()
        {
            videoPlayer.Stop();
            if (string.IsNullOrEmpty(currentVideoName))
            {
                return;
            }

            videoPlayer.clip = null;
            currentVideoName = null;
        }

        public void Play()
        {
            // Call Stop in case something strange is not reset by Play
            videoPlayer.Stop();
            videoPlayer.Play();
            if (videoPlayer.canSetDirectAudioVolume && videoPlayer.audioTrackCount > 0)
            {
                videoPlayer.SetDirectAudioVolume(0, volume);
            }
        }

        #endregion

        #region Restoration

        public string restorableName => luaName;

        [Serializable]
        private class VideoControllerRestoreData : IRestoreData
        {
            public readonly string currentVideoName;

            public VideoControllerRestoreData(VideoController parent)
            {
                currentVideoName = parent.currentVideoName;
            }
        }

        public IRestoreData GetRestoreData()
        {
            return new VideoControllerRestoreData(this);
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
