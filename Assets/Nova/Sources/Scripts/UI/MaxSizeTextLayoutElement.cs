using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nova
{
    [RequireComponent(typeof(Text))]
    [ExecuteInEditMode]
    public class MaxSizeTextLayoutElement : UIBehaviour, ILayoutElement
    {
        private Text text;

        [SerializeField] private float _maxWidth = -1f;
        [SerializeField] private float _maxHeight = -1f;

        protected override void Awake()
        {
            text = GetComponent<Text>();
        }

        public void CalculateLayoutInputHorizontal()
        {
            text.CalculateLayoutInputHorizontal();
        }

        public void CalculateLayoutInputVertical()
        {
            text.CalculateLayoutInputVertical();
        }

        public float minWidth => text.minWidth;

        public float minHeight => text.minHeight;

        public float preferredWidth => Mathf.Min(text.preferredWidth, maxWidth);

        public float preferredHeight => Mathf.Min(text.preferredHeight, maxHeight);

        public float flexibleWidth => text.flexibleWidth;

        public float flexibleHeight => text.flexibleHeight;

        public int layoutPriority => text.layoutPriority + 1;

        public float maxHeight
        {
            get => _maxHeight;
            set
            {
                if (Mathf.Approximately(_maxHeight, value)) return;
                _maxHeight = value;
                SetDirty();
            }
        }

        public float maxWidth
        {
            get => _maxWidth;
            set
            {
                if (Mathf.Approximately(_maxWidth, value)) return;
                _maxWidth = value;
                SetDirty();
            }
        }

        protected override void OnEnable()
        {
            SetDirty();
        }

        protected override void OnDisable()
        {
            SetDirty();
        }

        protected override void OnTransformParentChanged()
        {
            SetDirty();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            SetDirty();
        }

        private void SetDirty()
        {
            if (!IsActive())
            {
                return;
            }

            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            SetDirty();
        }
#endif
    }
}
