using UnityEngine;

namespace Nova
{
    public class ScreenCapturer : MonoBehaviour
    {
        private const float AspectRatio = 1.0f * Bookmark.ScreenShotWidth / Bookmark.ScreenShotHeight;

        public GameObject screenCameraObject;

        private Camera screenCamera;

        private void Start()
        {
            // Save the initial screen size. Because when viewport is resized, the screen camera size won't change.
            screenCamera = screenCameraObject.GetComponent<Camera>();
        }

        public Texture2D GetTexture()
        {
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            var containerSize = screenSize.GetContainerSize(AspectRatio);
            var texture = new Texture2D(containerSize.x, containerSize.y, TextureFormat.RGB24, false);
            var renderTexture = new RenderTexture(containerSize.x, containerSize.y, 0);

            screenCamera.targetTexture = renderTexture;
            screenCamera.Render();
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(Vector2.zero, containerSize), 0, 0, false);
            RenderTexture.active = null;
            screenCamera.targetTexture = null;
            Destroy(renderTexture);
            texture.Apply();

            TextureScale.Bilinear(texture, Bookmark.ScreenShotWidth, Bookmark.ScreenShotHeight);

            return texture;
        }
    }
}