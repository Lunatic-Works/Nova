using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    /// <summary>
    /// Central component to connect the render process and
    /// preserve aspect ratio by adding black margin around actual game view.
    /// It does the following jobs:
    /// * Connect game camera output to ui
    /// * Create a dummy final camera & render texture for final rendering
    /// * Calculate desired dimensions, set to RealScreen class, and update final camera rect
    /// * Make current main camera render to the render texture
    /// * Hijack the camera rendering process and directly blit the render texture to screen instead
    /// </summary>
    public class GameRenderManager : MonoBehaviour
    {
        private const string LastWindowedHeightKey = "_LastWindowedHeight";
        private const string LastWindowedWidthKey = "_LastWindowedWidth";
        private static readonly int GlobalRealScreenHeightID = Shader.PropertyToID("_GH");
        private static readonly int GlobalRealScreenWidthID = Shader.PropertyToID("_GW");
        private static readonly int Global1920ScaleID = Shader.PropertyToID("_GScale");
        private bool isLogicalFullScreen;

        private const string ChangeWindowSizeFirstShownKey = ConfigManager.FirstShownKeyPrefix + "ChangeWindowSize";

        public Color marginColor;
        public float desiredAspectRatio;
        public RawImage gameRenderTarget;
        public Toggle fullScreenToggle;

        private ConfigManager configManager;

        private int lastScreenHeight, lastScreenWidth;
        private Camera finalCamera;
        private RenderTexture gameRenderTexture, finalRenderTexture;
        private int shouldUpdateTransitionsAfter = -1;

        private void Awake()
        {
            this.RuntimeAssert(gameRenderTarget != null, "GameRenderTarget must be set.");
            this.RuntimeAssert(fullScreenToggle != null, "FullScreenToggle must be set.");

            Tag = tag;

            configManager = Utils.FindNovaGameController().ConfigManager;

            finalCamera = gameObject.AddComponent<Camera>();
            finalCamera.backgroundColor = marginColor;
            finalCamera.clearFlags = CameraClearFlags.SolidColor;
            finalCamera.cullingMask = 0;

            fullScreenToggle.isOn = isLogicalFullScreen = Screen.fullScreenMode == FullScreenMode.FullScreenWindow;
            fullScreenToggle.onValueChanged.AddListener(UpdateFullScreenStatus);

            UpdateDesiredDimensions();
        }

        private void OnDestroy()
        {
            fullScreenToggle.onValueChanged.RemoveListener(UpdateFullScreenStatus);
        }

        private void UpdateFullScreenStatus(bool to)
        {
            if (Application.isMobilePlatform)
            {
                return;
            }

            // Debug.LogFormat("Full screen status changing from {0} (logical {1}) to {2}", Screen.fullScreen, isLogicalFullScreen, to);
            if (isLogicalFullScreen == to)
            {
                return;
            }

            isLogicalFullScreen = to;
            if (to)
            {
                configManager.SetInt(LastWindowedWidthKey, Screen.width);
                configManager.SetInt(LastWindowedHeightKey, Screen.height);
                Screen.SetResolution(
                    Screen.currentResolution.width,
                    Screen.currentResolution.height,
                    FullScreenMode.FullScreenWindow
                );
            }
            else
            {
                var targetW = configManager.GetInt(LastWindowedWidthKey);
                var targetH = configManager.GetInt(LastWindowedHeightKey);
                if (targetW == 0 || targetH == 0)
                {
                    if (Screen.resolutions.Length == 0)
                    {
                        targetW = 1280;
                        targetH = 720;
                    }
                    else
                    {
                        var defaultResolution = Screen.resolutions[Screen.resolutions.Length / 2];
                        targetW = defaultResolution.width;
                        targetH = defaultResolution.height;
                    }
                }

                Screen.SetResolution(targetW, targetH, FullScreenMode.Windowed);

                if (configManager.GetInt(ChangeWindowSizeFirstShownKey) == 0)
                {
                    Alert.Show(I18n.__("config.changewindowsize"));
                    configManager.SetInt(ChangeWindowSizeFirstShownKey, 1);
                }
            }
        }

        private void UpdateDesiredDimensions()
        {
            if (lastScreenHeight == Screen.height && lastScreenWidth == Screen.width)
            {
                return;
            }

            // Debug.LogFormat("Resolution changed: {0}, {1}", Screen.height, Screen.width);
            lastScreenHeight = Screen.height;
            lastScreenWidth = Screen.width;

            RealScreen.aspectRatio = desiredAspectRatio;
            var aspectRatio = 1.0f * Screen.width / Screen.height;
            if (aspectRatio < desiredAspectRatio)
            {
                RealScreen.fHeight = Screen.width / desiredAspectRatio;
                RealScreen.height = (int)RealScreen.fHeight;
                RealScreen.fWidth = RealScreen.width = Screen.width;

                var delta = 1 - aspectRatio / RealScreen.aspectRatio;
                finalCamera.rect = Rect.MinMaxRect(
                    0, delta / 2, 1, 1 - delta / 2
                );
            }
            else
            {
                RealScreen.fWidth = Screen.height * desiredAspectRatio;
                RealScreen.width = (int)RealScreen.fWidth;
                RealScreen.fHeight = RealScreen.height = Screen.height;

                var delta = 1 - RealScreen.aspectRatio / aspectRatio;
                finalCamera.rect = Rect.MinMaxRect(
                    delta / 2, 0, 1 - delta / 2, 1
                );
            }

            Shader.SetGlobalFloat(GlobalRealScreenHeightID, RealScreen.fHeight);
            Shader.SetGlobalFloat(GlobalRealScreenWidthID, RealScreen.fWidth);
            Shader.SetGlobalFloat(Global1920ScaleID, RealScreen.fWidth / 1920);

            if (gameRenderTexture != null)
            {
                Destroy(gameRenderTexture);
            }

            if (finalRenderTexture != null)
            {
                Destroy(finalRenderTexture);
            }

            gameRenderTarget.texture = gameRenderTexture =
                new RenderTexture(RealScreen.width, RealScreen.height, 24)
                {
                    name = "GameRenderTexture"
                };

            foreach (var camera in GameObject.FindGameObjectsWithTag("MainCamera"))
            {
                camera.GetComponent<Camera>().targetTexture = gameRenderTexture;
            }

            UICameraHelper.Active.targetTexture = finalRenderTexture =
                new RenderTexture(RealScreen.width, RealScreen.height, 24)
                {
                    name = "FinalRenderTexture"
                };

            // Debug.LogFormat("Screen Size: {0} x {1}", RealScreen.width, RealScreen.height);

            shouldUpdateTransitionsAfter = 2;
        }

        private void Update()
        {
            UpdateDesiredDimensions();

            if (shouldUpdateTransitionsAfter >= 0)
            {
                foreach (var t in FindObjectsOfType<UIViewTransitionBase>())
                {
                    t.ResetTransitionTarget();
                }

                RealScreen.uiSize = gameRenderTarget.rectTransform.rect.size;
                shouldUpdateTransitionsAfter--;
            }
        }

        private void OnPreCull()
        {
            GL.Clear(true, true, marginColor, 0f);
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (finalRenderTexture != null)
            {
                Graphics.Blit(finalRenderTexture, dest);
            }
        }

        private void _switchFullScreen()
        {
            fullScreenToggle.isOn = !isLogicalFullScreen;
        }

        public static string Tag;

        public static void SwitchFullScreen()
        {
            var go = GameObject.FindWithTag(Tag);
            go.GetComponent<GameRenderManager>()._switchFullScreen();
        }
    }

    public static class UICameraHelper
    {
        public static Camera Active => GameObject.FindWithTag("UICamera").GetComponent<Camera>();
    }
}