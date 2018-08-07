using UnityEngine;

namespace Nova
{
    public class ScreenCapturer : MonoBehaviour
    {
        private const float AspectRatio = 1.0f * Bookmark.ScreenShotWidth / Bookmark.ScreenShotHeight;

        public GameObject screenCameraObject;

        private RenderTexture renderTexture;
        private Camera screenCamera;
        private Vector2Int initialScreenSize, containerSize, padding;

        private void Start()
        {
            // Save the initial screen size. Because when viewport is resized, the screen camera size won't change.
            initialScreenSize = new Vector2Int(Screen.width, Screen.height);
            containerSize = initialScreenSize.GetContainerSize(AspectRatio);
            padding = containerSize - initialScreenSize;

            screenCamera = screenCameraObject.GetComponent<Camera>();
            renderTexture = new RenderTexture(initialScreenSize.x, initialScreenSize.y, 0);
            screenCamera.targetTexture = renderTexture;
        }

        public Texture2D GetTexture()
        {
            Texture2D texture = new Texture2D(containerSize.x, containerSize.y, TextureFormat.RGB24, false);

            screenCamera.Render();
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(Vector2.zero, initialScreenSize), padding.x / 2, padding.y / 2, false);
            RenderTexture.active = null;
            texture.Apply();

            TextureScale.Bilinear(texture, Bookmark.ScreenShotWidth, Bookmark.ScreenShotHeight);

            return texture;
        }
    }
}