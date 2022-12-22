using Nova.Generate;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Nova
{
    /// <summary>
    /// Warn: currently it is impossible to restore texture property to its default value
    /// please manually set all texture to restore by name
    /// </summary>
    [ExportCustomType]
    public class RestorableMaterial : Material
    {
        // we have to record texture info explicitly
        private readonly Dictionary<string, string> textureNames = new Dictionary<string, string>();

        private static string GetLastShaderName(string s)
        {
            return s.Split('/').Last();
        }

        private static void Init(string shaderName)
        {
            if (ShaderInfoDatabase.TypeData.ContainsKey(GetLastShaderName(shaderName))) return;
            Debug.LogWarning($"Nova: Shader {shaderName} is not in the shader info database. " +
                             "The material will not be restored correctly.");
        }

        public RestorableMaterial(Shader shader) : base(shader)
        {
            Init(shader.name);
        }

        public RestorableMaterial(Material mat) : base(mat)
        {
            Init(shader.name);
        }

        public RestorableMaterial(RestorableMaterial resMat) : base(resMat)
        {
            textureNames = resMat.textureNames;
        }

        public new void SetTexture(string propertyName, Texture value)
        {
            Debug.LogWarning($"Nova: Setting a texture to {shader.name}:{propertyName} without a path to the texture. " +
                             "The material will not be restored correctly. Please use SetTexturePath instead.");
            base.SetTexture(propertyName, value);
        }

        public new void SetTexture(int id, Texture value)
        {
            Debug.LogWarning($"Nova: Setting a texture to {shader.name}:{id} without a path to texture. " +
                             "The material will not be restored correctly. Please use SetTexturePath instead.");
            base.SetTexture(id, value);
        }

        public void SetTexturePath(string propertyName, string texturePath)
        {
            if (texturePath == null)
            {
                texturePath = "";
            }

            if (texturePath.StartsWith(AssetLoader.RenderTargetPrefix, StringComparison.Ordinal))
            {
                var rtName = texturePath.Substring(AssetLoader.RenderTargetPrefix.Length);
                var rt = Utils.FindRenderManager().GetRenderTarget(rtName) as RenderTarget;
                rt.Bind(this, propertyName);
            }
            else
            {
                Texture tex = null;
                if (texturePath != "")
                {
                    tex = AssetLoader.Load<Texture>(texturePath);
                }

                base.SetTexture(propertyName, tex);
            }

            textureNames[propertyName] = texturePath;
        }

        private static void AddMaterialPropertyData(Material mat, MaterialData data,
            string name, ShaderPropertyType type)
        {
            switch (type)
            {
                case ShaderPropertyType.Color:
                    var c = mat.GetColor(name);
                    data.colorDatas.Add(name, c);
                    break;
                case ShaderPropertyType.Vector:
                    var v = mat.GetVector(name);
                    data.vectorDatas.Add(name, v);
                    break;
                case ShaderPropertyType.Float:
                    var f = mat.GetFloat(name);
                    data.floatDatas.Add(name, f);
                    break;
                case ShaderPropertyType.TexEnv:
                    var offset = mat.GetTextureOffset(name);
                    var scale = mat.GetTextureScale(name);
                    data.textureScaleOffsets.Add(name, new Vector4Data(scale, offset));
                    var res = mat as RestorableMaterial;
                    if (res == null) break;
                    if (!res.textureNames.TryGetValue(name, out string tn)) break;
                    data.textureNames.Add(name, tn);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static MaterialData GetRestoreData(Material material)
        {
            if (material == null || material.shader == null) return null;

            if (!ShaderInfoDatabase.TypeData.TryGetValue(GetLastShaderName(material.shader.name), out var properties))
            {
                Debug.LogWarning($"Nova: Shader {material.shader.name} is not in the shader info database. " +
                                 "The material will not be restored correctly.");
                return null;
            }

            var data = new MaterialData(material is RestorableMaterial, material.shader.name);
            foreach (var p in properties)
            {
                var name = p.Key;
                var type = p.Value;
                AddMaterialPropertyData(material, data, name, type);
            }

            return data;
        }

        public static Material Restore(MaterialData data, MaterialFactory factory)
        {
            Assert.IsNotNull(factory);
            // no data supplied, return nothing
            if (data == null) return null;

            // preserve data consistency of material factor
            var mat = data.isRestorableMaterial
                ? factory.GetRestorableMaterial(data.shaderName)
                : factory.Get(data.shaderName);
            foreach (var c in data.colorDatas)
            {
                mat.SetColor(c.Key, c.Value);
            }

            foreach (var v in data.vectorDatas)
            {
                mat.SetVector(v.Key, v.Value);
            }

            foreach (var f in data.floatDatas)
            {
                mat.SetFloat(f.Key, f.Value);
            }

            foreach (var so in data.textureScaleOffsets)
            {
                var name = so.Key;
                so.Value.Split(out var s, out var o);
                mat.SetTextureScale(name, s);
                mat.SetTextureOffset(name, o);
            }

            if (!data.isRestorableMaterial) return mat;

            var res = mat as RestorableMaterial;
            Assert.IsNotNull(res);
            foreach (var t in data.textureNames)
            {
                res.SetTexturePath(t.Key, t.Value);
            }

            return res;
        }
    }
}
