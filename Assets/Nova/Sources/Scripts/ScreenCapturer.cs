using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class ScreenCapturer : MonoBehaviour
    {
        [HideInInspector] public Texture2D gameTexture2D;

        private void Awake()
        {
            LuaRuntime.Instance.BindObject("screenCapturer", this);
        }

        public static RenderTexture GetGameTexture(bool withUI = true)
        {
            var screenCamera = withUI ? UICameraHelper.Active : Camera.main;
            var renderTexture = new RenderTexture(RealScreen.width, RealScreen.height, 24)
            {
                name = "ScreenCapturerRenderTexture"
            };

            screenCamera.RenderToTexture(renderTexture);

            return renderTexture;
        }

        public void SetGameTexture2D()
        {
            var screenSize = new Vector2Int(RealScreen.width, RealScreen.height);
            gameTexture2D = new Texture2D(RealScreen.width, RealScreen.height, TextureFormat.RGB24, false);
            var renderTexture = GetGameTexture(withUI: false);

            RenderTexture.active = renderTexture;
            gameTexture2D.ReadPixels(new Rect(Vector2.zero, screenSize), 0, 0, false);
            RenderTexture.active = null;
            Destroy(renderTexture);
            gameTexture2D.Apply();
        }

        public void DestroyGameTexture2D()
        {
            Destroy(gameTexture2D);
            gameTexture2D = null;
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

        private void OnDestroy()
        {
            DestroyGameTexture2D();
        }
    }
}