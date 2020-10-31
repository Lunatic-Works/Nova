using UnityEngine;

namespace Nova
{
    using BGMUnlockInfo = SerializableHashSet<string>;

    [RequireComponent(typeof(BGMGalleryController))]
    [ExportCustomType]
    public class BGMUnlockHelper : MonoBehaviour
    {
        private BGMGalleryController controller;
        private CheckpointManager checkpoint;

        private void Awake()
        {
            controller = GetComponent<BGMGalleryController>();
            checkpoint = Utils.FindNovaGameController().CheckpointManager;
            LuaRuntime.Instance.BindObject("bgmUnlockHelper", this);
        }

        private string statusKey => controller.bgmUnlockStatusKey;

        public void Unlock(string id)
        {
            var s = checkpoint.Get<BGMUnlockInfo>(statusKey) ?? new BGMUnlockInfo();
            if (s.Contains(id)) return;
            s.Add(id);
            checkpoint.Set(statusKey, s);
        }
    }
}