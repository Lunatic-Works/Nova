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

        [SerializeField] protected float fadeDuration = 0.1f;

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

        protected bool inited;

        protected virtual void Init()
        {
            if (inited)
            {
                return;
            }

            materialPool = gameObject.Ensure<MaterialPool>();
            fadeMaterial = materialPool.Get(fadeShader);
            novaAnimation = Utils.FindNovaController().PerDialogueAnimation;

            inited = true;
        }

        protected virtual void Awake()
        {
            Init();
        }

        protected void DoFadeAnimation(float duration)
        {
            fadeMaterial.SetColor(SubColorID, fadeMaterial.GetColor(ColorID));
            if (duration < EPS)
            {
                fadeMaterial.SetFloat(TimeID, 0.0f);
            }
            else
            {
                fadeMaterial.SetFloat(TimeID, 1.0f);
                novaAnimation.Then(new MaterialFloatAnimationProperty(fadeMaterial, TIME, 0.0f), duration);
            }
        }

        protected void DoFadeAnimation()
        {
            DoFadeAnimation(fadeDuration);
        }
    }
}
