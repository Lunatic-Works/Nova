using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace Nova
{
    using BGMUnlockInfo = SerializableHashSet<string>;

    public enum MusicListMode
    {
        Sequential = 0,
        SingleLoop = 1,
        Random = 2
    }

    public class BGMGalleryController : ViewControllerBase
    {
        private static readonly BGMUnlockInfo DefaultUnlockSet = new BGMUnlockInfo();
        private CheckpointManager checkpointManager;

        public BGMGalleryMusicPlayer musicPlayer;
        public string bgmUnlockStatusKey = "bgm_unlock_status";
        public MusicEntryList bgmList;

        public Transform bgmListScrollContent;
        public BGMGalleryEntry bgmEntryPrefab;
        public GameObject lockedBGMPrefab;
        public List<AudioController> audioControllersToDisable;

        // the index of entries in _allBGMs is their index in _unlockedBGMs
        // indices of those locked BGMs are -1
        private const int LockedIndex = -1;
        private List<MusicListEntry> allBgMs;
        private List<MusicListEntry> unlockedBgMs;

        private MusicListMode _mode = MusicListMode.Sequential;

        public MusicListMode mode
        {
            get => _mode;
            set
            {
                if (_mode == value) return;
                _mode = value;
                RefreshMusicPlayerList();
            }
        }

        private IMusicList GetMusicList(MusicListEntry currentMusic)
        {
            Assert.IsNotNull(currentMusic);
            if (unlockedBgMs.Count == 0)
            {
                return null;
            }

            switch (mode)
            {
                case MusicListMode.Sequential:
                    return new SequentialMusicList(unlockedBgMs, currentMusic.index);
                case MusicListMode.SingleLoop:
                    return new SingleLoopMusicList(unlockedBgMs, currentMusic.index);
                case MusicListMode.Random:
                    return new RandomMusicList(unlockedBgMs, currentMusic.index);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Play(MusicListEntry music)
        {
            musicPlayer.musicList = GetMusicList(music);
            musicPlayer.Play();
        }

        private void PauseOtherAudios()
        {
            foreach (var ac in audioControllersToDisable)
            {
                ac.Pause();
            }
        }

        private void RestoreOtherAudios()
        {
            foreach (var ac in audioControllersToDisable)
            {
                ac.UnPause();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            checkpointManager = Utils.FindNovaGameController().CheckpointManager;
            allBgMs = bgmList.entries
                .Select(entry => new MusicListEntry(LockedIndex, entry))
                .ToList();
        }

        public override void Show(Action onFinish)
        {
            PauseOtherAudios();
            RefreshContent();
            base.Show(onFinish);
        }

        public override void Hide(Action onFinish)
        {
            RestoreOtherAudios();
            musicPlayer.Pause();
            base.Hide(onFinish);
        }

        private void RefreshContent()
        {
            UpdateUnlockedBGMs();
            RefreshBGMListView();
            RefreshMusicPlayerList();
        }

        private void RefreshMusicPlayerList()
        {
            if (musicPlayer == null || musicPlayer.musicList == null) return;
            musicPlayer.musicList = GetMusicList(musicPlayer.musicList.Current());
        }

        private static bool IsUnlocked(MusicListEntry entry)
        {
            return entry.index >= 0;
        }

        private void UpdateUnlockedBGMs()
        {
            var unlockedInfo = checkpointManager.Get(bgmUnlockStatusKey, DefaultUnlockSet);
            unlockedBgMs = new List<MusicListEntry>();
            foreach (var bgm in allBgMs)
            {
                if (IsUnlocked(unlockedInfo, bgm.entry))
                {
                    bgm.index = unlockedBgMs.Count;
                    unlockedBgMs.Add(bgm);
                }
                else
                {
                    bgm.index = LockedIndex;
                }
            }
        }

        private void ClearBGMListView()
        {
            var children = new List<GameObject>();
            foreach (Transform child in bgmListScrollContent)
            {
                children.Add(child.gameObject);
            }

            foreach (var child in children)
            {
                Destroy(child);
            }
        }

        private static bool IsUnlocked(ICollection<string> unlockInfo, MusicEntry entry)
        {
            return unlockInfo.Contains(entry.id);
        }

        private void RefreshBGMListView()
        {
            ClearBGMListView();
            foreach (var bgm in allBgMs)
            {
                if (IsUnlocked(bgm))
                {
                    var entry = Instantiate(bgmEntryPrefab, bgmListScrollContent, false);
                    entry.Init(bgm, Play);
                }
                else
                {
                    Instantiate(lockedBGMPrefab, bgmListScrollContent, false);
                }
            }
        }

        private void UnlockAllBGMs()
        {
            var unlockHelper = GetComponent<BGMUnlockHelper>();
            if (!unlockHelper)
            {
                return;
            }

            foreach (var bgm in allBgMs)
            {
                unlockHelper.Unlock(bgm.entry.id);
            }

            RefreshContent();
        }

        protected override void OnActivatedUpdate()
        {
            base.OnActivatedUpdate();
            if (Utils.GetKeyDownInEditor(KeyCode.LeftShift))
            {
                UnlockAllBGMs();
            }
        }
    }
}