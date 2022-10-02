using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Nova
{
    /// Use MusicGalleryPlayer.Play and Pause instead of manipulating the underlying AudioSource directly
    /// MusicGalleryPlayer maintains an isPlaying flag, and it will be in sync with AudioSource.isPlaying
    /// If isPlaying is out of sync, that means the underlying clip has finished playing, and the player
    /// will play the next music in musicList
    public class MusicGalleryPlayer : MonoBehaviour
    {
        public AudioSource audioSource;
        public Text titleLabel;
        public MusicGalleryProgressBar progressBar;
        public GameObject playButton;
        public GameObject pauseButton;

        public bool isPlaying { get; private set; }

        private void ApplyInvalidMusicEntry()
        {
            audioSource.clip = null;
            titleLabel.text = I18n.__("musicgallery.title");
            progressBar.Init();
            progressBar.interactable = false;
        }

        private void ApplyMusicEntry(MusicEntry music)
        {
            Assert.IsNotNull(music);
            audioSource.clip = AssetLoader.Load<AudioClip>(music.resourcePath);
            titleLabel.text = music.GetDisplayName();
            progressBar.Init();
            progressBar.interactable = true;
        }

        private bool needResetMusicOffset = true;

        private MusicEntry _currentMusic;

        private MusicEntry currentMusic
        {
            get => _currentMusic;
            set
            {
                if (_currentMusic == value)
                {
                    return;
                }

                _currentMusic = value;
                needResetMusicOffset = true;
                Refresh();
            }
        }

        private IMusicList _musicList;

        public IMusicList musicList
        {
            get => _musicList;
            set
            {
                _musicList = value;
                currentMusic = _musicList?.Current()?.entry;
            }
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Refresh()
        {
            // Sync isPlaying flag on initialization
            Pause();

            if (currentMusic == null)
            {
                ApplyInvalidMusicEntry();
            }
            else
            {
                ApplyMusicEntry(currentMusic);
            }
        }

        public void Play()
        {
            if (audioSource.clip == null) return;
            if (audioSource.isPlaying) return;
            if (needResetMusicOffset)
            {
                audioSource.time = 0.0f;
                needResetMusicOffset = false;
            }

            isPlaying = true;
            audioSource.Play();

            playButton.SetActive(false);
            pauseButton.SetActive(true);
        }

        public void Pause()
        {
            isPlaying = false;
            audioSource.Pause();

            playButton.SetActive(true);
            pauseButton.SetActive(false);
        }

        public void Next()
        {
            if (musicList == null) return;
            Pause();
            needResetMusicOffset = true;
            currentMusic = musicList.Next().entry;
            Play();
        }

        private void Step()
        {
            if (musicList == null) return;
            Pause();
            needResetMusicOffset = true;
            currentMusic = musicList.Step().entry;
            Play();
        }

        public void Previous()
        {
            if (musicList == null) return;
            Pause();
            needResetMusicOffset = true;
            currentMusic = musicList.Previous().entry;
            Play();
        }

        private void Update()
        {
            if (audioSource.isPlaying == isPlaying) return;

            // Out of sync with the underlying AudioSource
            // Play the next music in musicList
            Assert.IsTrue(isPlaying);

            // Out of sync also happens when the application loses focus
            // Check the time to ensure the clip has finished playing
            if (audioSource.time < float.Epsilon || Mathf.Approximately(audioSource.time, audioSource.clip.length))
            {
                Step();
            }
        }
    }
}
