using System.Collections.Generic;
using System.Linq;
using Nova.URP;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Nova
{
    // if isActive, RenderManager will set targetTexture
    // if needUpdate, generate a new render texture
    // if isFinal, blit this to the screen
    public interface IRenderTargetConfig
    {
        RenderTexture targetTexture { get; set; }
        string textureName { get; }
        RenderTextureFormat textureFormat { get; }
        bool isFinal { get; }
        bool needUpdate { get; }
        bool isActive { get; }
    }

    /// <summary>
    /// Central component to connect the rendering process and
    /// preserve the aspect ratio by adding black margins around the game view.
    /// It does the following jobs:
    /// * Render UI over the game view
    /// * Create a dummy camera and a render texture for the final rendering
    /// * Calculate desired dimensions, set them to RealScreen, and update the final camera's rect
    /// * Make the current main camera render to the final render texture
    /// * Hijack the camera rendering process and directly blit the render texture to the screen instead
    /// </summary>
    [ExecuteInEditMode]
    public class RenderManager : OnPostRenderBehaviour
    {
        private const string LastWindowedHeightKey = "_LastWindowedHeight";
        private const string LastWindowedWidthKey = "_LastWindowedWidth";
        private static readonly int GlobalRealScreenHeightID = Shader.PropertyToID("_GH");
        private static readonly int GlobalRealScreenWidthID = Shader.PropertyToID("_GW");
        private static readonly int GlobalRealScreenScaleID = Shader.PropertyToID("_GScale");
        private const string SHADER = "Nova/Premul/Final Blit";
        private const string ChangeWindowSizeFirstShownKey = ConfigManager.FirstShownKeyPrefix + "ChangeWindowSize";

        [SerializeField] private Color marginColor;
        [SerializeField] private float desiredAspectRatio = 16.0f / 9.0f;
        [SerializeField] private float smoothTime = 0.1f;
        [SerializeField] private RawImage gameRenderTarget;
        [SerializeField] private ConfigToggle fullScreenToggle;

        private ConfigManager configManager;
        private Camera finalCamera;
        private Material material;
        private bool isLogicalFullScreen;
        private int lastScreenHeight, lastScreenWidth;
        private int lastRealHeight, lastRealWidth;
        private float screenHeightVelocity, screenWidthVelocity;
        private int shouldUpdateTexturesAfter = -1;
        private int shouldUpdateUIAfter = -1;
        private readonly List<IRenderTargetConfig> renderTargets = new List<IRenderTargetConfig>();
        private IRenderTargetConfig finalTarget => renderTargets.Find(rt => rt.isActive && rt.isFinal);

        private void Awake()
        {
            this.RuntimeAssert(gameRenderTarget != null, "GameRenderTarget must be set.");
            this.RuntimeAssert(fullScreenToggle != null, "FullScreenToggle must be set.");

            if (Application.isPlaying)
            {
                configManager = Utils.FindNovaController().ConfigManager;
                isLogicalFullScreen = Screen.fullScreenMode == FullScreenMode.FullScreenWindow;
                configManager.SetInt(fullScreenToggle.configKeyName, isLogicalFullScreen ? 1 : 0);
                configManager.AddValueChangeListener(fullScreenToggle.configKeyName, UpdateFullScreenStatus);
            }

            finalCamera = gameObject.Ensure<Camera>();
            finalCamera.backgroundColor = marginColor;
            finalCamera.clearFlags = CameraClearFlags.SolidColor;
            finalCamera.cullingMask = 0;
            finalCamera.depth = 0;
            finalCamera.rect = Rect.MinMaxRect(0, 0, 1, 1);

            var materialPool = gameObject.Ensure<MaterialPool>();
            material = materialPool.Get(SHADER);
            material.color = marginColor;

            UpdateScreen();
        }

        private void OnDestroy()
        {
            ClearRenderTargets();

            if (configManager != null)
            {
                configManager.RemoveValueChangeListener(fullScreenToggle.configKeyName, UpdateFullScreenStatus);
            }
        }

        private void Start()
        {
            // at this point, UI camera should be initialized
            UpdateUI();
        }

        private void Update()
        {
            UpdateScreen();
            UpdateTextures();
            UpdateUI();
        }

        #region Full screen

        private void UpdateFullScreenStatus()
        {
            if (Application.isMobilePlatform)
            {
                return;
            }

            // Debug.Log($"Change full screen status {Screen.fullScreen} (logical {isLogicalFullScreen}) -> {to}");
            var to = configManager.GetInt(fullScreenToggle.configKeyName) != 0;
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
                        // A conservative guess for the initial size
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
                    Alert.Show("config.changewindowsize");
                    configManager.SetInt(ChangeWindowSizeFirstShownKey, 1);
                }
            }
        }

        public void SwitchFullScreen()
        {
            configManager.SetInt(fullScreenToggle.configKeyName, isLogicalFullScreen ? 0 : 1);
        }

        #endregion

        #region Render target

        public override void ExecuteOnRenderImageFeature(ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            if (finalTarget == null)
            {
                return;
            }

            var cmd = CommandBufferPool.Get("Composition");
            if (finalTarget.targetTexture != null)
            {
                cmd.Blit(finalTarget.targetTexture, BuiltinRenderTextureType.CurrentActive, material);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public bool RegisterRenderTarget(IRenderTargetConfig target)
        {
            if (renderTargets.Find(rt => rt.textureName == target.textureName) != null)
            {
                return false;
            }

            UpdateScreen();
            if (target.isActive && RealScreen.isScreenInitialized)
            {
                UpdateTexture(target);
            }

            renderTargets.Add(target);
            return true;
        }

        public void UnregisterRenderTarget(string rtName)
        {
            var target = renderTargets.Find(rt => rt.textureName == rtName);
            if (target == null)
            {
                return;
            }

            renderTargets.Remove(target);
            UpdateTexture(target, null);
        }

        public IRenderTargetConfig GetRenderTarget(string rtName)
        {
            return renderTargets.Find(rt => rt.textureName == rtName);
        }

        private void ClearRenderTargets()
        {
            foreach (var rt in renderTargets)
            {
                UpdateTexture(rt, null);
            }

            renderTargets.Clear();
        }

        #endregion

        private void UpdateScreen()
        {
            if (RealScreen.isScreenInitialized && lastScreenHeight == Screen.height && lastScreenWidth == Screen.width)
            {
                return;
            }

            if (Screen.height <= 0 || Screen.width <= 0)
            {
                return;
            }

            var isSmoothResizing = false;
            if (lastScreenHeight <= 0 || lastScreenWidth <= 0 ||
                Mathf.Abs(Screen.height - lastScreenHeight) > 2 || Mathf.Abs(Screen.width - lastScreenWidth) > 2)
            {
                lastScreenHeight = Screen.height;
                lastScreenWidth = Screen.width;
            }
            else
            {
                var fHeight = Mathf.SmoothDamp(lastScreenHeight, Screen.height, ref screenHeightVelocity, smoothTime);
                var fWidth = Mathf.SmoothDamp(lastScreenWidth, Screen.width, ref screenWidthVelocity, smoothTime);
                lastScreenHeight = Mathf.RoundToInt(fHeight + Mathf.Clamp(Screen.height - fHeight, -1.0f, 1.0f));
                lastScreenWidth = Mathf.RoundToInt(fWidth + Mathf.Clamp(Screen.width - fWidth, -1.0f, 1.0f));
                isSmoothResizing = true;
            }

            var aspectRatio = (float)lastScreenWidth / lastScreenHeight;
            if (aspectRatio < desiredAspectRatio)
            {
                RealScreen.fHeight = lastScreenWidth / desiredAspectRatio;
                RealScreen.height = (int)RealScreen.fHeight;
                RealScreen.fWidth = RealScreen.width = lastScreenWidth;
            }
            else
            {
                RealScreen.fWidth = lastScreenHeight * desiredAspectRatio;
                RealScreen.width = (int)RealScreen.fWidth;
                RealScreen.fHeight = RealScreen.height = lastScreenHeight;
            }

            if (RealScreen.isScreenInitialized &&
                (lastRealWidth != RealScreen.width || lastRealHeight != RealScreen.height))
            {
                if (shouldUpdateTexturesAfter < 0)
                {
                    if (isSmoothResizing)
                    {
                        shouldUpdateTexturesAfter = 3;
                    }
                    else
                    {
                        shouldUpdateTexturesAfter = 1;
                    }
                }

                shouldUpdateUIAfter = 2;
            }

            lastRealWidth = RealScreen.width;
            lastRealHeight = RealScreen.height;
            RealScreen.isScreenInitialized = true;
            Shader.SetGlobalFloat(GlobalRealScreenHeightID, RealScreen.fHeight);
            Shader.SetGlobalFloat(GlobalRealScreenWidthID, RealScreen.fWidth);
            Shader.SetGlobalFloat(GlobalRealScreenScaleID, RealScreen.scale);

            // Debug.Log($"Update screen {lastScreenWidth}x{lastScreenHeight} -> {RealScreen.width}x{RealScreen.height} {isSmoothResizing}");
        }

        private static void UpdateTexture(IRenderTargetConfig rt)
        {
            var texture = RenderTexture.GetTemporary(RealScreen.width, RealScreen.height, 0, rt.textureFormat);
            texture.name = rt.textureName;
            UpdateTexture(rt, texture);
        }

        private static void UpdateTexture(IRenderTargetConfig rt, RenderTexture texture)
        {
            var oldTexture = rt.targetTexture;
            rt.targetTexture = texture;
            if (oldTexture)
            {
                RenderTexture.ReleaseTemporary(oldTexture);
            }

            // var verb = texture == null ? "Destroy" : "Update";
            // Debug.Log($"{verb} render texture {rt.textureName}");
        }

        private void UpdateTextures()
        {
            if (shouldUpdateTexturesAfter > 0)
            {
                shouldUpdateTexturesAfter--;
            }

            foreach (var rt in renderTargets)
            {
                if (rt.isActive)
                {
                    if (shouldUpdateTexturesAfter == 0 || rt.needUpdate || rt.targetTexture == null)
                    {
                        UpdateTexture(rt);
                    }
                }
                else
                {
                    if (rt.targetTexture != null)
                    {
                        UpdateTexture(rt, null);
                    }
                }
            }

            if (shouldUpdateTexturesAfter == 0)
            {
                shouldUpdateTexturesAfter = -1;
            }
        }

        private void UpdateUI()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (shouldUpdateUIAfter > 0)
            {
                shouldUpdateUIAfter--;
            }

            if (shouldUpdateUIAfter == 0)
            {
                RealScreen.uiSize = gameRenderTarget.rectTransform.rect.size;
                RealScreen.isUIInitialized = true;
                foreach (var trans in FindObjectsOfType<UIViewTransitionBase>().Where(x => !x.inAnimation))
                {
                    trans.ResetTransitionTarget();
                }

                shouldUpdateUIAfter = -1;

                // Debug.Log($"Update UI {RealScreen.uiSize}");
            }
        }
    }

    public class UICameraHelper
    {
        private static UICameraHelper _instance;

        public static Camera Active
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UICameraHelper
                    {
                        camera = GameObject.FindWithTag("UICamera").GetComponent<Camera>()
                    };
                }

                return _instance.camera;
            }
        }

        private Camera camera;
    }
}
