using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using Nova.URP;

namespace Nova
{
    public class CompositeSpriteRenderer : MonoBehaviour, IOnRenderImage
    {
        public string mergerTag = "SpriteMerger1";

        public RenderPassEvent renderPassEvent => RenderPassEvent.BeforeRenderingTransparents;

        public void ExecuteOnRenderImageFeature(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var gos = GameObject.FindGameObjectsWithTag(mergerTag);

            var cmd = CommandBufferPool.Get("Render Composite Sprite");
            foreach (var go in gos)
            {
                var renderTarget = go.GetComponent<CompositeSpriteRenderTarget>();
                if (renderTarget == null)
                {
                    continue;
                }
                renderTarget.Render(cmd);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
