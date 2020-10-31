using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [RequireComponent(typeof(RawImage))]
    public class UIOverlayTextureChanger : OverlayTextureChangerBase
    {
        private RawImage rawImage;

        protected override void Awake()
        {
            rawImage = GetComponent<RawImage>();
            base.Awake();
            rawImage.material = material;
        }

        protected override void ResetSize(float width, float height, Vector2 pivot)
        {
            if (float.IsNaN(width) || float.IsNaN(height))
            {
                rawImage.enabled = false;
                return;
            }

            rawImage.enabled = true;
        }
    }
}