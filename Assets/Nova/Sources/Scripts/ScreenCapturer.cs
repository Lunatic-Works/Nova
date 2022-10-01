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
        public static RenderTexture GetGameTexture(RenderTexture renderTexture = null, bool withUI = true)
        {
            RenderTexture oldRenderTexture = null;
            if (renderTexture == null || renderTexture.width != RealScreen.width || renderTexture.height != RealScreen.height)
            {
                oldRenderTexture = renderTexture;
                renderTexture = new RenderTexture(RealScreen.width, RealScreen.height, 24)
                {
                    name = "ScreenCapturerRenderTexture"
                };
            }

            var screenCamera = withUI ? UICameraHelper.Active : Camera.main;
            screenCamera.RenderToTexture(renderTexture);

            // Destroy oldRenderTexture after capturing, because it may be showing on the screen
            Destroy(oldRenderTexture);

            return renderTexture;
        }

        public void CaptureGameTexture()
        {
            capturedGameTexture = GetGameTexture(capturedGameTexture, withUI: false);
        }

        public static Texture2D GetBookmarkThumbnailTexture()
        {
            var texture = new Texture2D(Bookmark.ScreenshotWidth, Bookmark.ScreenshotHeight, TextureFormat.RGB24,
                false);
            var fullSizedRenderTexture = RenderTexture.GetTemporary(RealScreen.width, RealScreen.height, 24);
            var renderTexture = RenderTexture.GetTemporary(Bookmark.ScreenshotWidth, Bookmark.ScreenshotHeight, 24);

            UICameraHelper.Active.RenderToTexture(fullSizedRenderTexture);

            Graphics.Blit(fullSizedRenderTexture, renderTexture);
            RenderTexture.ReleaseTemporary(fullSizedRenderTexture);

            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, Bookmark.ScreenshotWidth, Bookmark.ScreenshotHeight), 0, 0, false);
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(renderTexture);
            texture.Apply();

            return texture;
        }
    }
}
