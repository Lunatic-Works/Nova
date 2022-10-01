using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Nova.URP
{
    public class OnRenderImageFeature : ScriptableRendererFeature
    {
        public static RenderTargetIdentifier GetCurrentTarget(ref RenderingData renderingData)
        {
            return renderingData.cameraData.renderer.cameraColorTarget;
        }

        private class CustomRenderPass : ScriptableRenderPass
        {
            public CustomRenderPass(RenderPassEvent renderPassEvent)
            {
                this.renderPassEvent = renderPassEvent;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var camera = renderingData.cameraData.camera;

                foreach (var b in camera.GetComponents<IOnRenderImage>())
                {
                    if (b.renderPassEvent == renderPassEvent)
                    {
                        b.ExecuteOnRenderImageFeature(context, ref renderingData);
                    }
                }
            }
        }

        private CustomRenderPass[] allPasses;

        private static readonly RenderPassEvent[] allEvents =
        {
            RenderPassEvent.AfterRendering,
            RenderPassEvent.BeforeRenderingTransparents,
        };

        /// <inheritdoc/>
        public override void Create()
        {
            allPasses = allEvents.Select(x => new CustomRenderPass(x)).ToArray();
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            foreach (var pass in allPasses)
            {
                renderer.EnqueuePass(pass);
            }
        }
    }
}
