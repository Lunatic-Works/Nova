using UnityEngine;

namespace Nova
{
    public abstract class FadeController : MonoBehaviour
    {
        private const string TIME = "_T";
        private const float EPS = 1e-6f;

        private static readonly int TimeID = Shader.PropertyToID(TIME);
        private static readonly int ColorID = Shader.PropertyToID("_Color");
        private static readonly int SubColorID = Shader.PropertyToID("_SubColor");

        public float fadeDuration = 0.1f;

        protected MaterialPool materialPool;
        protected NovaAnimation novaAnimation;

        public Material fadeMaterial { get; protected set; }
        protected bool isFading => fadeMaterial.GetFloat(TimeID) >= EPS;
        protected abstract string fadeShader { get; }

        public virtual Color color
        {
            get => fadeMaterial.GetColor(ColorID);
            set => fadeMaterial.SetColor(ColorID, value);
        }

        protected virtual void Awake()
        {
            materialPool = gameObject.Ensure<MaterialPool>();
            fadeMaterial = materialPool.Get(fadeShader);
            materialPool.defaultMaterial = fadeMaterial;
            novaAnimation = Utils.FindNovaController().PerDialogueAnimation;
        }

        protected void FadeAnimation(float delay)
        {
            fadeMaterial.SetColor(SubColorID, fadeMaterial.GetColor(ColorID));
            if (delay < EPS)
            {
                fadeMaterial.SetFloat(TimeID, 0.0f);
            }
            else
            {
                fadeMaterial.SetFloat(TimeID, 1.0f);
                novaAnimation.Do(new MaterialFloatAnimationProperty(fadeMaterial, TIME, 0.0f), fadeDuration);
            }
        }
    }
}
