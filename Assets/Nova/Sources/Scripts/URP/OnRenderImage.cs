using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Nova.URP
{
    public interface IOnRenderImage
    {
        RenderPassEvent renderPassEvent { get; }
        void ExecuteOnRenderImageFeature(ScriptableRenderContext context, ref RenderingData renderingData);
    }

    public abstract class OnPostRenderBehaviour : MonoBehaviour, IOnRenderImage
    {
        public RenderPassEvent renderPassEvent => RenderPassEvent.AfterRendering;

        public abstract void ExecuteOnRenderImageFeature(ScriptableRenderContext context,
            ref RenderingData renderingData);
    }
}
