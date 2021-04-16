using UnityEngine;

namespace Nova
{
    public class ConfigPanel : MonoBehaviour
    {
        public ConfigTextPreviewController textPreview;
        public NovaAnimation textPreviewAnimation;

        private void Awake()
        {
            textPreview.textPreviewAnimation = textPreviewAnimation;
        }
    }
}