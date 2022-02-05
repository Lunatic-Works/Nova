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

        private readonly List<List<Material>> layers = new List<List<Material>>();

        private int layersEnabledUntil; // Layers with id in [0, this) are enabled

        private int layerCount
        {
            get => layers.Count;
            set
            {
                if (value < layers.Count)
                    layers.Clear();
                for (int i = layers.Count; i < value; i++)
                    layers.Add(new List<Material>());
                layersEnabledUntil = value;
            }
        }

        public void PushMaterial(Material material)
        {
            PushMaterial(0, material);
        }

        public void PushMaterial(int layerID, Material material)
        {
            if (!material)
                return;
            if (layerCount <= layerID)
                layerCount = layerID + 1;
            layers[layerID].Add(material);
        }

        public void ClearLayer(int layerID = 0)
        {
            if (layerCount <= layerID)
                layerCount = layerID + 1;
            layers[layerID].Clear();
        }

        private bool LayerShouldRender(int layerID)
        {
            return layers[layerID].Count > 0;
        }

        private IEnumerable<Material> EnabledMaterials()
        {
            for (var i = 0; i < layersEnabledUntil; i++)
            {
                if (!LayerShouldRender(i)) continue;
                foreach (var mat in layers[i])
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
            public readonly List<List<MaterialRestoreData>> layersData;
            public readonly int layersEnabledUntil;

            public PostProcessingRestoreData(List<List<MaterialRestoreData>> layersData, int layersEnabledUntil)
            {
                this.layersData = layersData;
                this.layersEnabledUntil = layersEnabledUntil;
            }
        }

        public string restorableObjectName => luaName;

        public IRestoreData GetRestoreData()
        {
            // Materials must be RestorableMaterial
            var layersData = new List<List<MaterialRestoreData>>();
            foreach (var layer in layers)
            {
                var layerData = new List<MaterialRestoreData>();
                foreach (var material in layer)
                {
                    if (material is RestorableMaterial)
                    {
                        layerData.Add(RestorableMaterial.GetRestoreData(material));
                    }
                }

                layersData.Add(layerData);
            }

            return new PostProcessingRestoreData(layersData, layersEnabledUntil);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as PostProcessingRestoreData;

            // Materials must be RestorableMaterial
            MaterialFactory factory = MaterialPool.Ensure(gameObject).factory;
            layers.Clear();
            foreach (var layerData in data.layersData)
            {
                var layer = new List<Material>();
                foreach (var materialRestoreData in layerData)
                {
                    var material = RestorableMaterial.RestoreMaterialFromData(materialRestoreData, factory);
                    layer.Add(material);
                }

                layers.Add(layer);
            }

            layersEnabledUntil = data.layersEnabledUntil;
        }

        #endregion
    }
}