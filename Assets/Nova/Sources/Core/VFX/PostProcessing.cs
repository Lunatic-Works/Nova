using System;
using System.Collections.Generic;
using System.Linq;
using Nova.URP;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Nova
{
    [ExportCustomType]
    public class PostProcessing : OnPostRenderBehaviour, IRestorable
    {
        public string luaName;

        private GameState gameState;

        private void Awake()
        {
            gameState = Utils.FindNovaGameController().GameState;

            if (!string.IsNullOrEmpty(luaName))
            {
                LuaRuntime.Instance.BindObject(luaName, this);
                gameState.AddRestorable(this);
            }
        }

        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(luaName))
            {
                gameState.RemoveRestorable(this);
            }
        }

        private readonly List<Material> layers = new List<Material>();

        public void SetLayer(int layerID, Material material)
        {
            while (layers.Count <= layerID)
            {
                layers.Add(null);
            }

            layers[layerID] = material;
        }

        public void ClearLayer(int layerID)
        {
            if (layers.Count > layerID)
            {
                layers[layerID] = null;
            }
            else
            {
                Debug.LogWarning(
                    "Post processing layer already cleared. Maybe a trans is overwritten by another. " +
                    $"layerID: {layerID}, layers.Count: {layers.Count}");
            }

            while (layers.Count > 0 && layers[layers.Count - 1] == null)
            {
                layers.RemoveAt(layers.Count - 1);
            }
        }

        private IEnumerable<Material> EnabledMaterials()
        {
            foreach (var mat in layers)
            {
                if (mat)
                {
                    yield return mat;
                }
            }
        }

        public static readonly int TempBlitId = Shader.PropertyToID("_NovaTempBlit");

        public void Blit(CommandBuffer cmd, RenderTargetIdentifier renderTarget)
        {
            if (!EnabledMaterials().Any())
            {
                return;
            }

            RenderTargetIdentifier[] buffers = { TempBlitId, renderTarget };
            var from = 1;

            foreach (var mat in EnabledMaterials())
            {
                cmd.Blit(buffers[from], buffers[1 - from], mat);
                from = 1 - from;
            }

            if (from == 0)
            {
                cmd.Blit(buffers[0], buffers[1]);
            }
        }

        public override void ExecuteOnRenderImageFeature(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!EnabledMaterials().Any())
            {
                return;
            }

            var cmd = CommandBufferPool.Get("Nova Post Processing");
            var cam = renderingData.cameraData.camera;
            cmd.GetTemporaryRT(TempBlitId, cam.scaledPixelWidth, cam.scaledPixelHeight, 0);

            Blit(cmd, OnRenderImageFeature.DefaultCameraTarget);

            cmd.ReleaseTemporaryRT(TempBlitId);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        #region Restoration

        public string restorableName => luaName;

        [Serializable]
        private class PostProcessingRestoreData : IRestoreData
        {
            public readonly List<MaterialData> layersData;

            public PostProcessingRestoreData(List<MaterialData> layersData)
            {
                this.layersData = layersData;
            }
        }

        public IRestoreData GetRestoreData()
        {
            // Materials must be RestorableMaterial
            var layersData = layers.Select(RestorableMaterial.GetRestoreData).ToList();
            return new PostProcessingRestoreData(layersData);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as PostProcessingRestoreData;

            // Materials must be RestorableMaterial
            MaterialFactory factory = gameObject.Ensure<MaterialPool>().factory;
            layers.Clear();
            foreach (var materialData in data.layersData)
            {
                var material = RestorableMaterial.Restore(materialData, factory);
                layers.Add(material);
            }
        }

        #endregion
    }
}
