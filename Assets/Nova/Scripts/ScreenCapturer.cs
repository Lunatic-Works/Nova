using UnityEngine;

namespace Nova
{
    public class ScreenCapturer : MonoBehaviour
    {
        public GameObject screenCameraObject;

        private RenderTexture renderTexture;
        private Camera screenCamera;

        private void Start()
        {
            renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
            screenCamera = screenCameraObject.GetComponent<Camera>();
            screenCamera.targetTexture = renderTexture;
        }

        public Texture2D GetTexture()
        {
            Texture2D texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

            screenCamera.Render();
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
            RenderTexture.active = null;

            // Resize to bookmark's size
            // TODO: clipping
            // texture.Resize(Bookmark.ScreenShotWidth, Bookmark.ScreenShotHeight);

            texture.Apply();
            return texture;
        }
    }
}