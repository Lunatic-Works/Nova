using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [ExportCustomType]
    public class RenderQueueOverrider : MonoBehaviour
    {
        public int renderQueue;

        private new Renderer renderer;
        private Image image;

        private void Awake()
        {
            if (!TryGetComponent<Renderer>(out renderer))
            {
                if (TryGetComponent<IOverlayRenderer>(out var textureChanger))
                {
                    renderer = textureChanger.overlay.GetComponent<Renderer>();
                }
            }

            image = GetComponent<Image>();

            // Create MaterialPool to keep an instance of defaultMaterial
            gameObject.Ensure<MaterialPool>();

            renderQueue = GetRenderQueue();
        }

        private int GetRenderQueue()
        {
            if (renderer != null)
            {
                if (renderer.sharedMaterial != null)
                {
                    return renderer.sharedMaterial.renderQueue;
                }
            }
            else if (image != null)
            {
                if (image.material != null)
                {
                    return image.material.renderQueue;
                }
            }

            return -1;
        }

        private void SetRenderQueue(int value)
        {
            if (renderer != null)
            {
                if (renderer.sharedMaterial != null)
                {
                    renderer.sharedMaterial.renderQueue = value;
                }
            }
            else if (image != null)
            {
                if (image.material != null)
                {
                    image.material.renderQueue = value;
                }
            }
        }

        public void LateUpdate()
        {
            SetRenderQueue(renderQueue);
        }

        // Export to Lua
        public static RenderQueueOverrider Ensure(GameObject gameObject)
        {
            return gameObject.Ensure<RenderQueueOverrider>();
        }
    }
}
