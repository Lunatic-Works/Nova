using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class PostProcessing : MonoBehaviour, IRestorable
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

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            var matCnt = EnabledMaterials().Count();
            if (matCnt == 0)
            {
                Graphics.Blit(src, dest);
                return;
            }

            if (matCnt == 1)
            {
                var mat = EnabledMaterials().First();
                Graphics.Blit(src, dest, mat);
                return;
            }

            // double buffer pattern
            RenderTexture[] buffers = {RenderTexture.GetTemporary(src.descriptor), dest};
            // dest might be null
            // for matCnt == 2, src -> buffers[0] -> dest, fine
            // issue will occur when matCnt >= 3
            var destUseTmp = dest == null && matCnt >= 3;
            if (destUseTmp)
            {
                buffers[1] = RenderTexture.GetTemporary(src.descriptor);
            }

            // the final result is in buffers[1]
            var from = matCnt % 2;
            var first = true;
            foreach (var mat in EnabledMaterials())
            {
                if (first)
                {
                    Graphics.Blit(src, buffers[from], mat);
                    first = false;
                    continue;
                }

                Graphics.Blit(buffers[from], buffers[1 - from], mat);
                from = 1 - from;
            }

            if (destUseTmp)
            {
                Graphics.Blit(buffers[1], dest);
                RenderTexture.ReleaseTemporary(buffers[1]);
            }

            RenderTexture.ReleaseTemporary(buffers[0]);
        }

        #region Restoration

        [Serializable]
        private class PostProcessingRestoreData : IRestoreData
        {
            public readonly List<MaterialRestoreData> layersData;

            public PostProcessingRestoreData(List<MaterialRestoreData> layersData)
            {
                this.layersData = layersData;
            }
        }

        public string restorableObjectName => luaName;

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
            MaterialFactory factory = MaterialPool.Ensure(gameObject).factory;
            layers.Clear();
            foreach (var materialRestoreData in data.layersData)
            {
                var material = RestorableMaterial.RestoreMaterialFromData(materialRestoreData, factory);
                layers.Add(material);
            }
        }

        #endregion
    }
}