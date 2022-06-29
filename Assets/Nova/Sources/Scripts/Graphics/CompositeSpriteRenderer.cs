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
        private static readonly int PrimaryTexID = Shader.PropertyToID("_PrimaryTex");
        private static readonly int SubTexID = Shader.PropertyToID("_SubTex");

        public string mergerTag = "SpriteMerger1";

        public RenderPassEvent renderPassEvent => RenderPassEvent.BeforeRenderingTransparents;

        public void ExecuteOnRenderImageFeature(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var gos = GameObject.FindGameObjectsWithTag(mergerTag);

            var width = renderingData.cameraData.camera.scaledPixelWidth;
            var height = renderingData.cameraData.camera.scaledPixelHeight;

            var cmd = CommandBufferPool.Get("Render Composite Sprite");
            cmd.GetTemporaryRT(PrimaryTexID, width, height, 0);
            cmd.GetTemporaryRT(SubTexID, width, height, 0);
            foreach (var go in gos)
            {
                var controller = go.GetComponent<CompositeSpriteController>();
                if (controller != null && controller.renderTexture != null)
                {
                    controller.mergerPrimary.Render(cmd, PrimaryTexID);
                    controller.mergerSub.Render(cmd, SubTexID);
                    cmd.SetRenderTarget(controller.renderTexture);
                    cmd.ClearRenderTarget(true, true, Color.clear);
                    cmd.Blit(BuiltinRenderTextureType.None, controller.renderTexture, controller.fadeMaterial);
                }
            }
            cmd.ReleaseTemporaryRT(PrimaryTexID);
            cmd.ReleaseTemporaryRT(SubTexID);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
