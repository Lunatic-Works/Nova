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

        private readonly List<Material> layers = new List<Material>();

        // The owner of each VFX can hold a token to uniquely label the VFX,
        // even if two VFXs can reuse the same material
        // When clearing a layer, the owner can provide the token to check if
        // the layer is the intended VFX, and we do not clear it if the layer
        // is overwritten by another VFX
        // The automatic creation and removal of VFX only happens inside
        // transitions, so we don't need to restore the tokens
        private readonly Dictionary<int, int> tokens = new Dictionary<int, int>();
        private int lastToken;

        private void Awake()
        {
            gameState = Utils.FindNovaController().GameState;

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

        public int SetLayer(int layerID, Material material)
        {
            while (layers.Count <= layerID)
            {
                layers.Add(null);
            }

            layers[layerID] = material;

            tokens[layerID] = lastToken;
            ++lastToken;
            return tokens[layerID];
        }

        public void ClearLayer(int layerID, int token = -1)
        {
            if (layers.Count <= layerID || (token >= 0 && !tokens.ContainsKey(layerID)))
            {
                Debug.LogWarning(
                    "Nova: Post processing layer already cleared. Maybe a trans is overwritten by another. " +
                    $"layerID: {layerID}, layers.Count: {layers.Count}, expected token: {token}");
                return;
            }

            if (token >= 0 && tokens[layerID] != token)
            {
                Debug.LogWarning(
                    "Nova: Token not match when clearing post processing layer. " +
                    "Maybe a trans is overwritten by another. " +
                    $"layerID: {layerID}, layers.Count: {layers.Count}, expected token: {token}, " +
                    $"actual token: {tokens[layerID]}");
                return;
            }

            layers[layerID] = null;
            while (layers.Count > 0 && layers[layers.Count - 1] == null)
            {
                layers.RemoveAt(layers.Count - 1);
            }

            tokens.Remove(layerID);
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

            Blit(cmd, OnRenderImageFeature.GetCurrentTarget(ref renderingData));

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
