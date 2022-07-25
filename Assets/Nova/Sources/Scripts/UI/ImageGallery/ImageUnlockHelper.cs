using UnityEngine;

namespace Nova
{
    using ImageUnlockInfo = SerializableHashSet<string>;

    [ExportCustomType]
    [RequireComponent(typeof(ImageGalleryController))]
    public class ImageUnlockHelper : MonoBehaviour
    {
        private CheckpointManager checkpointManager;

        private void Awake()
        {
            checkpointManager = Utils.FindNovaGameController().CheckpointManager;
            LuaRuntime.Instance.BindObject("imageUnlockHelper", this);
        }

        private static string ImageUnlockStatusKey => ImageGalleryController.ImageUnlockStatusKey;

        public void Unlock(string path)
        {
            Debug.Log($"unlock {path}");
            path = Utils.ConvertPathSeparator(path);
            var unlockInfo = checkpointManager.Get(ImageUnlockStatusKey, new ImageUnlockInfo());
            if (unlockInfo.Contains(path)) return;
            unlockInfo.Add(path);
            checkpointManager.Set(ImageUnlockStatusKey, unlockInfo);
        }
    }
}
