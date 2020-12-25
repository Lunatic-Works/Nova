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

        private void Awake()
        {
            controller = GetComponent<MusicGalleryController>();
            checkpoint = Utils.FindNovaGameController().CheckpointManager;
            LuaRuntime.Instance.BindObject("musicUnlockHelper", this);
        }

        private string statusKey => controller.musicUnlockStatusKey;

        public void Unlock(string id)
        {
            var s = checkpoint.Get<MusicUnlockInfo>(statusKey) ?? new MusicUnlockInfo();
            if (s.Contains(id)) return;
            s.Add(id);
            checkpoint.Set(statusKey, s);
        }
    }
}