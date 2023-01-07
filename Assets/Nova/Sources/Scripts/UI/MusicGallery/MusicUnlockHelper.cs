using UnityEngine;

namespace Nova
{
    using MusicUnlockInfo = SerializableHashSet<string>;

    [ExportCustomType]
    [RequireComponent(typeof(MusicGalleryController))]
    public class MusicUnlockHelper : MonoBehaviour
    {
        private CheckpointManager checkpointManager;

        private void Awake()
        {
            checkpointManager = Utils.FindNovaController().CheckpointManager;
            LuaRuntime.Instance.BindObject("musicUnlockHelper", this);
        }

        private static string MusicUnlockStatusKey => MusicGalleryController.MusicUnlockStatusKey;

        public void Unlock(string path)
        {
            path = Utils.ConvertPathSeparator(path);
            var unlockInfo = checkpointManager.Get(MusicUnlockStatusKey, new MusicUnlockInfo());
            if (unlockInfo.Contains(path)) return;
            unlockInfo.Add(path);
            checkpointManager.Set(MusicUnlockStatusKey, unlockInfo);
        }
    }
}
