using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Nova
{
    /// use MusicGalleryPlayer.Play and MusicGalleryPlayer.Pause instead of manipulate
    /// the underlying AudioSource directly
    /// MusicGalleryPlayer will maintain an isPlaying status, it will be sync with AudioSource.isPlaying
    /// If isPlaying flag is out of sync, it means the underlying clip has finished playing. The player
    /// will play the next music in its playing list
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
            progressBar.interactable = false;
        }

        private void ApplyMusicEntry(MusicEntry music)
        {
            Assert.IsNotNull(music);
            audioSource.clip = AssetLoader.Load<AudioClip>(music.resourcePath);
            titleLabel.text = music.GetDisplayName();
            progressBar.interactable = true;
        }

        private void Start()
        {
            // it should wait for other components to be initialized
            Pause(); // sync IsPlaying flag on initialization
            ApplyInvalidMusicEntry();
        }

        private bool needResetMusicOffset = true;

        private MusicEntry _currentMusic;

        private MusicEntry currentMusic
        {
            get => _currentMusic;
            set
            {
                if (_currentMusic == value)
                    return;
                _currentMusic = value;
                needResetMusicOffset = true;
                Pause();
                if (value == null)
                {
                    ApplyInvalidMusicEntry();
                }
                else
                {
                    ApplyMusicEntry(value);
                }
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
            if (currentMusic != null)
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
                audioSource.time = 0;
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
            // out of sync with the underlying AudioSource
            // play the next song in the play list
            Assert.IsTrue(isPlaying);
            // out of sync also happens when the application lost focus
            // check the time to ensure the clip has finished playing
            if (audioSource.time < float.Epsilon || Mathf.Approximately(audioSource.time, audioSource.clip.length))
            {
                Step();
            }
        }
    }
}