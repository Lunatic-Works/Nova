using System.Collections.Generic;
using System.Linq;
using Nova.URP;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Nova
{
    // a render texture config
    // RenderManager will set targetTexture if isActive
    // if needUpdate, will generate new texture
    // if isFinal, RenderManager will Blit this to final screen
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
    /// Central component to connect the render process and
    /// preserve aspect ratio by adding black margin around actual game view.
    /// It does the following jobs:
    /// * Connect game camera output to ui
    /// * Create a dummy final camera & render texture for final rendering
    /// * Calculate desired dimensions, set to RealScreen class, and update final camera rect
    /// * Make current main camera render to the render texture
    /// * Hijack the camera rendering process and directly blit the render texture to screen instead
    /// </summary>
    [ExecuteInEditMode]
    public class RenderManager : OnPostRenderBehaviour
    {
        private const string LastWindowedHeightKey = "_LastWindowedHeight";
        private const string LastWindowedWidthKey = "_LastWindowedWidth";
        private static readonly int GlobalRealScreenHeightID = Shader.PropertyToID("_GH");
        private static readonly int GlobalRealScreenWidthID = Shader.PropertyToID("_GW");
        private static readonly int Global1920ScaleID = Shader.PropertyToID("_GScale");
        private const string SHADER = "Nova/VFX/Final Blit";
        private const string ChangeWindowSizeFirstShownKey = ConfigManager.FirstShownKeyPrefix + "ChangeWindowSize";

        public Color marginColor;
        public float desiredAspectRatio;
        public RawImage gameRenderTarget;
        public Toggle fullScreenToggle;

        private ConfigManager configManager;
        private Camera finalCamera;
        private Material material;
        private bool isLogicalFullScreen;
        private bool needUpdateTexture = false;
        private int lastScreenHeight, lastScreenWidth;
        private int lastRealHeight, lastRealWidth;
        private int shouldUpdateUIAfter = -1;
        private readonly List<IRenderTargetConfig> renderTargets = new List<IRenderTargetConfig>();
        private IRenderTargetConfig finalTarget => renderTargets.Find(rt => rt.isActive && rt.isFinal);

        private void Awake()
        {
            this.RuntimeAssert(gameRenderTarget != null, "GameRenderTarget must be set.");
            this.RuntimeAssert(fullScreenToggle != null, "FullScreenToggle must be set.");

            Tag = tag;

            if (Application.isPlaying)
            {
                configManager = Utils.FindNovaController().ConfigManager;

                fullScreenToggle.isOn = isLogicalFullScreen = Screen.fullScreenMode == FullScreenMode.FullScreenWindow;
                fullScreenToggle.onValueChanged.AddListener(UpdateFullScreenStatus);
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
            foreach (var rt in renderTargets)
            {
                UpdateTexture(rt, null);
            }

            renderTargets.Clear();
            fullScreenToggle.onValueChanged.RemoveListener(UpdateFullScreenStatus);
        }

        private void Start()
        {
            // at this point, UI camera should be initialized
            if (Application.isPlaying)
            {
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            RealScreen.uiSize = gameRenderTarget.rectTransform.rect.size;
            RealScreen.isUIInitialized = true;
            foreach (var trans in FindObjectsOfType<UIViewTransitionBase>().Where(x => !x.inAnimation))
            {
                trans.ResetTransitionTarget();
            }

            // Debug.Log($"Update UI {RealScreen.uiSize}");
        }

        private void OnPreRender()
        {
            UpdateDesiredDimensions();
            if (shouldUpdateUIAfter > 0)
            {
                shouldUpdateUIAfter--;
                if (shouldUpdateUIAfter == 0)
                {
                    UpdateUI();
                }
            }
        }

        private void UpdateFullScreenStatus(bool to)
        {
            if (Application.isMobilePlatform)
            {
                return;
            }

            // Debug.Log($"Change full screen status from {Screen.fullScreen} (logical {isLogicalFullScreen}) to {to}");
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
                    Alert.Show("config.changewindowsize");
                    configManager.SetInt(ChangeWindowSizeFirstShownKey, 1);
                }
            }
        }

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
            // Debug.Log($"find {rtName}");
            return renderTargets.Find(rt => rt.textureName == rtName);
        }

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

            lastScreenHeight = Screen.height;
            lastScreenWidth = Screen.width;
            RealScreen.aspectRatio = desiredAspectRatio;
            var aspectRatio = 1.0f * lastScreenWidth / lastScreenHeight;
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
                needUpdateTexture = true;
            }

            lastRealWidth = RealScreen.width;
            lastRealHeight = RealScreen.height;
            RealScreen.isScreenInitialized = true;
            Shader.SetGlobalFloat(GlobalRealScreenHeightID, RealScreen.fHeight);
            Shader.SetGlobalFloat(GlobalRealScreenWidthID, RealScreen.fWidth);
            Shader.SetGlobalFloat(Global1920ScaleID, RealScreen.scale);
            // Debug.Log($"Update Screen {lastScreenWidth}x{lastScreenHeight} => {RealScreen.width}x{RealScreen.height}");
        }

        private void UpdateTexture(IRenderTargetConfig rt)
        {
            var texture = RenderTexture.GetTemporary(RealScreen.width, RealScreen.height, 0, rt.textureFormat);
            texture.name = rt.textureName;
            UpdateTexture(rt, texture);
        }

        private void UpdateTexture(IRenderTargetConfig rt, RenderTexture texture)
        {
            var oldTexture = rt.targetTexture;
            rt.targetTexture = texture;
            if (oldTexture)
            {
                RenderTexture.ReleaseTemporary(oldTexture);
            }

            // var verb = texture == null ? "Destroy" : "Update";
            // Debug.Log($"{verb} renderTexture {rt.textureName}");
        }

        private void Update()
        {
            UpdateScreen();
            OnPreRender();
        }

        private void UpdateDesiredDimensions()
        {
            if (needUpdateTexture)
            {
                if (Application.isPlaying)
                {
                    shouldUpdateUIAfter = 2;
                }
            }

            foreach (var rt in renderTargets)
            {
                if (!rt.isActive && rt.targetTexture != null)
                {
                    UpdateTexture(rt, null);
                }
                else if (rt.isActive && (needUpdateTexture || rt.needUpdate || rt.targetTexture == null))
                {
                    UpdateTexture(rt);
                }
            }

            needUpdateTexture = false;
        }

        private void _switchFullScreen()
        {
            fullScreenToggle.isOn = !isLogicalFullScreen;
        }

        public static string Tag;

        public static void SwitchFullScreen()
        {
            var go = GameObject.FindWithTag(Tag);
            go.GetComponent<RenderManager>()._switchFullScreen();
        }
    }

    public static class UICameraHelper
    {
        public static Camera Active => GameObject.FindWithTag("UICamera").GetComponent<Camera>();
    }
}
