using Nova.URP;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Nova
{
    public class CompositeSpriteRenderer : MonoBehaviour, IOnRenderImage
    {
        private static readonly int PrimaryTexID = Shader.PropertyToID("_PrimaryTex");
        private static readonly int SubTexID = Shader.PropertyToID("_SubTex");

        [SerializeField] private string mergerTag = "";

        public RenderPassEvent renderPassEvent => RenderPassEvent.BeforeRenderingTransparents;

        private static void Render(CompositeSpriteController controller, CommandBuffer cmd,
            RenderTargetIdentifier target)
        {
            controller.mergerPrimary.Render(cmd, PrimaryTexID);
            controller.mergerSub.Render(cmd, SubTexID);
            cmd.SetRenderTarget(target);
            cmd.ClearRenderTarget(true, true, Color.clear);
            cmd.Blit(BuiltinRenderTextureType.None, target, controller.fadeMaterial);

            if (controller.TryGetComponent<PostProcessing>(out var postProcessing))
            {
                postProcessing.Blit(cmd, target);
            }
        }

        public void ExecuteOnRenderImageFeature(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var gos = GameObject.FindGameObjectsWithTag(mergerTag);
            var camera = renderingData.cameraData.camera;
            var width = camera.scaledPixelWidth;
            var height = camera.scaledPixelHeight;
            var cameraTarget = OnRenderImageFeature.GetCurrentTarget(ref renderingData);

            var cmd = CommandBufferPool.Get("Render Composite Sprite");
            cmd.GetTemporaryRT(PrimaryTexID, width, height, 0);
            cmd.GetTemporaryRT(SubTexID, width, height, 0);
            cmd.GetTemporaryRT(PostProcessing.TempBlitID, width, height, 0);
            foreach (var go in gos)
            {
                if (go.TryGetComponent<CompositeSpriteController>(out var controller) &&
                    (controller.layer == -1 || ((1 << controller.layer) & camera.cullingMask) != 0))
                {
                    if (controller.renderToCamera)
                    {
                        Render(controller, cmd, cameraTarget);
                    }
                    else if (controller.renderTexture != null)
                    {
                        Render(controller, cmd, controller.renderTexture);
                    }
                }
            }

            cmd.ReleaseTemporaryRT(PrimaryTexID);
            cmd.ReleaseTemporaryRT(SubTexID);
            cmd.ReleaseTemporaryRT(PostProcessing.TempBlitID);
            // Manually reset default render target.
            // The render target is not automatically restored on some render backend :(
            cmd.SetRenderTarget(cameraTarget);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
