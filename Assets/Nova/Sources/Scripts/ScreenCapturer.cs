using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class ScreenCapturer : MonoBehaviour
    {
        [HideInInspector] public RenderTexture capturedGameTexture;

        private void Awake()
        {
            LuaRuntime.Instance.BindObject("screenCapturer", this);
        }

        private void OnDestroy()
        {
            Destroy(capturedGameTexture);
        }

        // Will reuse renderTexture if possible, otherwise destroy it
        public static RenderTexture GetGameTexture(RenderTexture renderTexture = null, Camera camera = null)
        {
            RenderTexture oldRenderTexture = null;
            if (renderTexture == null || renderTexture.width != RealScreen.width ||
                renderTexture.height != RealScreen.height)
            {
                oldRenderTexture = renderTexture;
                renderTexture = new RenderTexture(RealScreen.width, RealScreen.height, 24)
                {
                    name = "ScreenCapturerRenderTexture"
                };
            }

            if (camera == null)
            {
                camera = Camera.main;
            }

            camera.RenderToTexture(renderTexture);

            // Destroy oldRenderTexture after capturing, because it may be showing on the screen
            Destroy(oldRenderTexture);

            return renderTexture;
        }

        public void CaptureGameTexture(Camera camera)
        {
            capturedGameTexture = GetGameTexture(capturedGameTexture, camera);
        }

        // material should use a PP shader and scale with RealScreen
        public static Texture2D GetBookmarkThumbnailTexture(Material material = null)
        {
            var fullSizedRenderTexture = RenderTexture.GetTemporary(RealScreen.width, RealScreen.height, 24);
            GetGameTexture(fullSizedRenderTexture, UICameraHelper.Active);

            var renderTexture = RenderTexture.GetTemporary(Bookmark.ScreenshotWidth, Bookmark.ScreenshotHeight, 24);
            Graphics.Blit(fullSizedRenderTexture, renderTexture, material);
            RenderTexture.ReleaseTemporary(fullSizedRenderTexture);

            var texture = new Texture2D(Bookmark.ScreenshotWidth, Bookmark.ScreenshotHeight, TextureFormat.RGB24,
                false);
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, Bookmark.ScreenshotWidth, Bookmark.ScreenshotHeight), 0, 0, false);
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(renderTexture);
            texture.Apply();

            return texture;
        }
    }
}
