using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    public interface ITextureReceiver
    {
        void SetTexture(RenderTexture texture);
    }

    // It seems intriguing to make this a MonoBehaviour
    // but in case when a gameObject needs multiple render targets it is not so good
    // Current design mode is to make a nested class inheriting this one and
    // being a member of MonoBehaviour
    [ExportCustomType]
    public abstract class RenderTarget : IRenderTargetConfig
    {
        public const string SUFFIX = "Texture";

        protected TextureRendererConfig oldConfig;
        protected RenderManager renderManager;
        protected bool _needUpdate = true;
        protected bool registered = false;
        protected RenderTexture _targetTexture;

        // these binding are not persist
        // need extra mechanism (e.g. RestorableMaterial or RawImageController to be restorable)
        protected readonly List<ITextureReceiver> receivers = new List<ITextureReceiver>();

        public abstract string textureName { get; }
        public virtual RenderTextureFormat textureFormat => RenderTextureFormat.ARGB32;
        public abstract bool isFinal { get; }
        public bool needUpdate => _needUpdate;
        public abstract bool isActive { get; }

        public virtual RenderTexture targetTexture
        {
            get => _targetTexture;
            set
            {
                _targetTexture = value;
                _needUpdate = false;

                // avoid the case when the receiver is destroyed
                var oldRecvs = receivers.Where(x => x.IsNotNullOrDestroyed()).ToList();
                foreach (var recv in oldRecvs)
                {
                    recv.SetTexture(_targetTexture);
                }
            }
        }

        public virtual void Awake()
        {
            renderManager = Utils.FindRenderManager();
            registered = renderManager.RegisterRenderTarget(this);
            oldConfig = new TextureRendererConfig(textureName, textureFormat, isFinal);
        }

        public virtual void Update()
        {
            var newConfig = new TextureRendererConfig(textureName, textureFormat, isFinal);
            if (oldConfig != newConfig)
            {
                _needUpdate = true;
                oldConfig = newConfig;
            }
        }

        public virtual void OnDestroy()
        {
            if (registered)
            {
                // prevent from using textureName directly
                renderManager.UnregisterRenderTarget(oldConfig.name);
            }
        }

        private class MaterialBinder : ITextureReceiver
        {
            private readonly RenderTarget renderTarget;
            public readonly Material mat;
            public readonly string texName;
            private bool bound;

            public MaterialBinder(RenderTarget renderTarget, Material mat, string texName)
            {
                this.renderTarget = renderTarget;
                this.mat = mat;
                this.texName = texName;
            }

            private void SetMatTexture(RenderTexture texture)
            {
                // var x = texture == null ? "null" : texture.name;
                // Debug.Log($"setMatTexture {this}=>{x}");
                mat.SetTexture(texName, texture);
            }

            public void SetTexture(RenderTexture texture)
            {
                if (mat != null)
                {
                    var oldTex = mat.GetTexture(texName);
                    var matchOld = oldTex != null && oldTex.name == renderTarget.textureName;
                    if (texture == null)
                    {
                        if (matchOld)
                        {
                            SetMatTexture(null);
                        }

                        renderTarget.Unbind(this);
                    }
                    else if (bound && !matchOld)
                    {
                        // unbind because the texture of the material is updated
                        renderTarget.Unbind(this);
                    }
                    else
                    {
                        SetMatTexture(texture);
                        bound = true;
                    }
                }
                else
                {
                    renderTarget.Unbind(this);
                }
            }

            public override string ToString()
            {
                return $"{mat.name}.{texName}";
            }
        }

        private static bool IsMaterialBinder(ITextureReceiver recv, Material mat, string texName)
        {
            return recv is MaterialBinder m && m.mat == mat && m.texName == texName;
        }

        public void Bind(ITextureReceiver recv)
        {
            if (!receivers.Contains(recv))
            {
                receivers.Add(recv);
            }
        }

        public void Unbind(ITextureReceiver recv)
        {
            // Debug.Log($"unbind {recv} => {textureName}");
            receivers.Remove(recv);
        }

        public void Bind(Material material, string texName)
        {
            // Debug.Log($"bind {material.name}.{texName} => {textureName}");
            if (receivers.Find(x => IsMaterialBinder(x, material, texName)) == null)
            {
                var binder = new MaterialBinder(this, material, texName);
                if (_targetTexture != null)
                {
                    binder.SetTexture(_targetTexture);
                }

                receivers.Add(binder);
            }
        }

        public void Unbind(Material material, string texName)
        {
            receivers.RemoveAll(x => IsMaterialBinder(x, material, texName));
        }
    }

    [Serializable]
    public class TextureRendererConfig
    {
        public readonly string name;
        public readonly RenderTextureFormat format;
        public readonly bool final;

        public TextureRendererConfig(string name, RenderTextureFormat format, bool final)
        {
            this.name = name;
            this.format = format;
            this.final = final;
        }

        public override bool Equals(object obj)
        {
            return obj is TextureRendererConfig config &&
                   name == config.name &&
                   format == config.format &&
                   final == config.final;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = name.GetHashCode();
                hashCode = hashCode * -1521134295 + final.GetHashCode();
                hashCode = hashCode * -1521134295 + format.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(TextureRendererConfig a, TextureRendererConfig b) => a.Equals(b);

        public static bool operator !=(TextureRendererConfig a, TextureRendererConfig b) => !(a == b);
    }
}
