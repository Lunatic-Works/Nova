using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    public sealed class MaterialFactory : IDisposable
    {
        private readonly Dictionary<string, Material> materials;
        private readonly Dictionary<string, RestorableMaterial> restorableMaterials;

        public MaterialFactory()
        {
            materials = new Dictionary<string, Material>();
            restorableMaterials = new Dictionary<string, RestorableMaterial>();
        }

        public Material Get(string shaderName)
        {
            if (materials.TryGetValue(shaderName, out var mat)) return mat;

            var shader = Shader.Find(shaderName);
            if (shader == null)
                throw new ArgumentException($"Nova: Shader not found: {shaderName}");

            mat = new Material(shader)
            {
                name = string.Format("Nova - {0}",
                    shaderName.Substring(shaderName.IndexOf("/", StringComparison.Ordinal) + 1)),
                hideFlags = HideFlags.DontSave
            };

            materials.Add(shaderName, mat);
            return mat;
        }

        public RestorableMaterial GetRestorableMaterial(string shaderName)
        {
            if (restorableMaterials.TryGetValue(shaderName, out var resMat)) return resMat;

            var shader = Shader.Find(shaderName);
            if (shader == null)
                throw new ArgumentException($"Nova: Shader not found: {shaderName}");

            resMat = new RestorableMaterial(shader)
            {
                name = string.Format("Nova:Restorable - {0}",
                    shaderName.Substring(shaderName.IndexOf("/", StringComparison.Ordinal) + 1)),
                hideFlags = HideFlags.DontSave
            };

            restorableMaterials.Add(shaderName, resMat);
            return resMat;
        }

        public void Dispose()
        {
            foreach (var m in materials.Values)
            {
                Utils.DestroyObject(m);
            }

            materials.Clear();

            foreach (var m in restorableMaterials.Values)
            {
                Utils.DestroyObject(m);
            }

            restorableMaterials.Clear();
        }
    }
}
