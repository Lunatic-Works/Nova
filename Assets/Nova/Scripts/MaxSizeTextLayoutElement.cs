using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nova
{
    [RequireComponent(typeof(Text))]
    [ExecuteInEditMode]
    public class MaxSizeTextLayoutElement : UIBehaviour, ILayoutElement
    {
        private Text _text;
        [SerializeField] private float m_maxWidth = -1f;
        [SerializeField] private float m_maxHeight = -1f;

        private new void Awake()
        {
            _text = GetComponent<Text>();
        }

        public void CalculateLayoutInputHorizontal()
        {
            _text.CalculateLayoutInputHorizontal();
        }

        public void CalculateLayoutInputVertical()
        {
            _text.CalculateLayoutInputVertical();
        }

        public float minWidth
        {
            get { return _text.minWidth; }
        }

        public float minHeight
        {
            get { return _text.minHeight; }
        }

        public float preferredWidth
        {
            get { return _text.preferredWidth > this.m_maxWidth ? this.m_maxWidth : _text.preferredWidth; }
        }

        public float preferredHeight
        {
            get { return _text.preferredHeight > this.m_maxHeight ? this.m_maxHeight : _text.preferredHeight; }
        }

        public float flexibleWidth
        {
            get { return _text.flexibleWidth; }
        }

        public float flexibleHeight
        {
            get { return _text.flexibleHeight; }
        }

        public int layoutPriority
        {
            get { return _text.layoutPriority + 1; }
        }

        public float maxHeight
        {
            get { return m_maxHeight; }
            set
            {
                Debug.Log("Changed");
                if (Math.Abs(m_maxHeight - value) < 1e-2) return;
                m_maxHeight = value;
                this.SetDirty();
            }
        }

        public float maxWidth
        {
            get { return m_maxWidth; }
            set
            {
                if (Math.Abs(m_maxWidth - value) < 1e-2) return;
                m_maxWidth = value;
                this.SetDirty();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            this.SetDirty();
        }

        protected override void OnTransformParentChanged()
        {
            this.SetDirty();
        }

        /// <summary>
        ///   <para>See MonoBehaviour.OnDisable.</para>
        /// </summary>
        protected override void OnDisable()
        {
            this.SetDirty();
            base.OnDisable();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            this.SetDirty();
        }

        protected override void OnBeforeTransformParentChanged()
        {
            this.SetDirty();
        }

        /// <summary>
        ///   <para>Mark the LayoutElement as dirty.</para>
        /// </summary>
        private void SetDirty()
        {
            if (!this.IsActive())
                return;
            LayoutRebuilder.MarkLayoutForRebuild(this.transform as RectTransform);
        }

        protected override void OnValidate()
        {
            this.SetDirty();
        }
    }
}