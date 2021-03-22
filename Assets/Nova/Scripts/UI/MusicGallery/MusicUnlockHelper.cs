using UnityEngine;

namespace Nova
{
    using MusicUnlockInfo = SerializableHashSet<string>;

    [RequireComponent(typeof(MusicGalleryController))]
    [ExportCustomType]
    public class MusicUnlockHelper : MonoBehaviour
    {
        private MusicGalleryController controller;
        private CheckpointManager checkpoint;
        private MusicUnlockInfo unlockInfo;

        private void Awake()
        {
            controller = GetComponent<MusicGalleryController>();
            checkpoint = Utils.FindNovaGameController().CheckpointManager;
            LuaRuntime.Instance.BindObject("musicUnlockHelper", this);
        }

        private void Start()
        {
            unlockInfo = checkpoint.Get<MusicUnlockInfo>(statusKey) ?? new MusicUnlockInfo();
        }

        private string statusKey => controller.musicUnlockStatusKey;

        public void Unlock(string path)
        {
            path = Utils.ConvertPathSeparator(path);
            if (unlockInfo.Contains(path)) return;
            unlockInfo.Add(path);
            checkpoint.Set(statusKey, unlockInfo);
        }
    }
}