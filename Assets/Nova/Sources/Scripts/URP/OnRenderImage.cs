using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Nova.URP
{
    public interface IOnRenderImage
    {
        void ExecuteOnRenderImageFeature(ScriptableRenderContext context, ref RenderingData renderingData);
        RenderPassEvent renderPassEvent { get; }
    }

    public abstract class OnPostRenderBehaviour : MonoBehaviour, IOnRenderImage
    {
        public RenderPassEvent renderPassEvent => RenderPassEvent.AfterRendering;

        public abstract void ExecuteOnRenderImageFeature(ScriptableRenderContext context, ref RenderingData renderingData);
    }
}
