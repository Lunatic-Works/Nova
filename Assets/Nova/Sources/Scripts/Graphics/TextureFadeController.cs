using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    public abstract class TextureFadeController : FadeController
    {
        private static readonly int PrimaryTextureID = Shader.PropertyToID("_PrimaryTex");
        private static readonly int SubTextureID = Shader.PropertyToID("_SubTex");
        private static readonly int OffsetsID = Shader.PropertyToID("_Offsets");

        private Texture lastTexture;

        protected override string fadeShader => "Nova/VFX/Change Texture With Fade";

        protected abstract void ResetSize(float width, float height, Vector2 pivot);

        protected void SetTexture(Texture to, float delay)
        {
            if (lastTexture == to)
            {
                return;
            }

            var offsets =
                GetTextureOffsetsAlignedOnAnchor(out Vector2 rendererSize, out Vector2 pivot, new[] { to, lastTexture });

            ResetSize(rendererSize.x, rendererSize.y, pivot);

            // material is not RestorableMaterial
            fadeMaterial.SetVector(OffsetsID, new Vector4(offsets[0].x, offsets[0].y, offsets[1].x, offsets[1].y));
            fadeMaterial.SetTexture(PrimaryTextureID, to);
            fadeMaterial.SetTextureScale(PrimaryTextureID,
                to != null ? rendererSize.InverseScale(new Vector2(to.width, to.height)) : Vector2.zero);
            fadeMaterial.SetTexture(SubTextureID, lastTexture);
            fadeMaterial.SetTextureScale(
                SubTextureID,
                lastTexture != null
                    ? rendererSize.InverseScale(new Vector2(lastTexture.width, lastTexture.height))
                    : Vector2.zero
            );
            FadeAnimation(delay);
            lastTexture = to;
        }

        public void SetTexture(Texture to)
        {
            SetTexture(to, fadeDuration);
        }

        public void SetTextureNoFade(Texture to)
        {
            SetTexture(to, 0f);
        }

        private static List<Vector2> GetTextureOffsetsAlignedOnAnchor(out Vector2 boundingSize,
            out Vector2 boundingAnchor, IReadOnlyList<Texture> textures, IReadOnlyList<Vector2> pivots = null)
        {
            var anchorDistances = new Vector4();
            for (int i = 0; i < textures.Count; i++)
            {
                var t = textures[i];
                if (t == null)
                {
                    continue;
                }

                float w = t.width, h = t.height;
                var pivot = new Vector2(w / 2, h / 2);
                if (pivots != null)
                {
                    pivot = pivots[i];
                }

                anchorDistances.x = Mathf.Max(pivot.x, anchorDistances.x);
                anchorDistances.y = Mathf.Max(pivot.y, anchorDistances.y);
                anchorDistances.z = Mathf.Max(w - pivot.x, anchorDistances.z);
                anchorDistances.w = Mathf.Max(h - pivot.y, anchorDistances.w);
            }

            boundingSize = new Vector2(anchorDistances.x + anchorDistances.z, anchorDistances.y + anchorDistances.w);
            boundingAnchor = new Vector2(anchorDistances.x, anchorDistances.y);
            var result = new List<Vector2>();
            for (int i = 0; i < textures.Count; i++)
            {
                var t = textures[i];
                if (t == null)
                {
                    result.Add(Vector2.zero);
                    continue;
                }

                float w = t.width, h = t.height;
                var pivot = new Vector2(w / 2, h / 2);
                if (pivots != null)
                {
                    pivot = pivots[i];
                }

                result.Add(new Vector2(
                    (anchorDistances.x - pivot.x) / t.width,
                    (anchorDistances.y - pivot.y) / t.height
                ));
            }

            return result;
        }
    }
}
