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
            renderer = GetComponent<Renderer>();
            if (renderer == null)
            {
                var textureChanger = GetComponent<GameOverlayTextureChanger>();
                if (textureChanger != null)
                {
                    renderer = textureChanger.actualImageObject.GetComponent<Renderer>();
                }
            }

            image = GetComponent<Image>();

            // Create MaterialPool to keep an instance of DefaultMaterial
            MaterialPool.Ensure(gameObject);

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

        public static RenderQueueOverrider Ensure(GameObject gameObject)
        {
            var overrider = gameObject.GetComponent<RenderQueueOverrider>();
            if (overrider == null)
            {
                overrider = gameObject.AddComponent<RenderQueueOverrider>();
            }

            return overrider;
        }
    }
}