using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nova
{
    [Serializable]
    public class ButtonRingItem
    {
        public UnityEvent action;
        public Sprite activeSprite;
        public string actionI18nName;
    }

    public class ButtonRing : MonoBehaviour
    {
        [SerializeField] private List<ButtonRingItem> _sectors = new List<ButtonRingItem>();
        [SerializeField] private float _sectorRadius = 200.0f;
        private List<GameObject> sectorObjects = new List<GameObject>(); // sectorObjects.Count = sectors.Count + 1;

        [Range(0, 1)] public float innerRatio = 0.3f;
        [Range(-180, 180)] public float angleOffset = -67.5f;
        public Sprite defaultSprite;
        public Text actionNameText;

        private UIViewTransitionBase transition;
        private Canvas currentCanvas;

        private void Awake()
        {
            currentCanvas = GetComponentInParent<Canvas>();
            transition = GetComponent<UIViewTransitionBase>();
            UpdateAllSectors();
        }

        public List<ButtonRingItem> sectors
        {
            get => _sectors;
            set
            {
                _sectors = value;
                UpdateAllSectors();
            }
        }

        public float sectorRadius
        {
            get => _sectorRadius;
            set
            {
                _sectorRadius = value;
                var rt = GetComponent<RectTransform>();
                Vector2 v;
                v.x = _sectorRadius;
                v.y = _sectorRadius;
                rt.offsetMin = -v;
                rt.offsetMax = v;
            }
        }

        public void SuppressNextAction()
        {
            suppressNextAction = true;
        }

        private GameObject CreateSectorObject(Sprite sprite)
        {
            var go = new GameObject("ButtonRingSector");
            go.SetActive(false);

            var img = go.AddComponent<Image>();
            img.sprite = sprite;

            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return go;
        }

        private void UpdateAllSectors()
        {
            foreach (var go in sectorObjects)
            {
                Destroy(go);
            }

            // Assert.AreEqual(transform.childCount, 0);

            sectorObjects = new List<GameObject>
            {
                CreateSectorObject(defaultSprite)
            };
            sectorObjects.AddRange(_sectors.Select(sector => CreateSectorObject(sector.activeSprite)));
        }

        private Vector2 GetAnchorPosToCanvas()
        {
            var t = transform;
            var pos = Vector2.zero;
            while (!t.TryGetComponent<Canvas>(out _))
            {
                var rect = t.GetComponent<RectTransform>();
                pos += rect.anchoredPosition;
                t = t.parent;
            }

            return pos;
        }

        private Vector2 preCalculatedAnchorPos;

        private int selectedSectorIndex = -1;

        public void BeginEntryAnimation()
        {
            transform.localScale = Vector3.one;
            transition.ResetTransitionTarget();
            transition.Enter(null);
        }

        private void OnEnable()
        {
            preCalculatedAnchorPos = GetAnchorPosToCanvas();
            selectedSectorIndex = -1;
            actionNameText.text = "";
            actionNameText.fontSize = (int)(_sectorRadius / 6);
        }

        private bool suppressNextAction = true;

        private void OnDisable()
        {
            if (suppressNextAction)
            {
                suppressNextAction = false;
                selectedSectorIndex = -1;
                return;
            }

            if (selectedSectorIndex != -1 && selectedSectorIndex < sectors.Count)
            {
                sectors[selectedSectorIndex].action.Invoke();
            }
        }

        /// <summary>
        /// return relative angle in deg, range from [0, 360), distance will be the distance from pointer to anchor point
        /// </summary>
        /// <returns>return relative angle in deg, range from [0, 360)</returns>
        private float CalculatePointerRelative(out float distance)
        {
            Vector2 pointerPos = currentCanvas.ScreenToCanvasPosition(RealInput.pointerPosition);
            var anchorPos = preCalculatedAnchorPos;
            var diff = pointerPos - anchorPos;
            distance = diff.magnitude;
            var angle = Mathf.Atan2(diff.y, diff.x);
            if (angle < 0f)
            {
                // angle ranges in [0, 2 PI)
                angle = Mathf.PI * 2f + angle;
            }

            // cvt to reg in [0, 360)
            angle *= Mathf.Rad2Deg;

            return angle;
        }

        /// <summary>
        /// Relative angle between [0, 360)
        /// </summary>
        /// <param name="angle">relative angle</param>
        /// <returns>the index of sector being hovered. if no sector is hovered, return -1</returns>
        private int GetSectorIndexAtAngle(float angle)
        {
            var sectorRange = 360f / _sectors.Count;
            var index = Mathf.FloorToInt(angle / sectorRange);
            if (index < 0)
            {
                index += _sectors.Count;
            }

            return index;
        }

        private void Update()
        {
            var pointerRelativeAngle = CalculatePointerRelative(out var distance);
            if (float.IsNaN(pointerRelativeAngle))
            {
                return;
            }

            if (distance < innerRatio * _sectorRadius)
            {
                selectedSectorIndex = -1;
                actionNameText.text = "";
            }
            else
            {
                selectedSectorIndex = GetSectorIndexAtAngle(pointerRelativeAngle + angleOffset);
                actionNameText.text = I18n.__(_sectors[selectedSectorIndex].actionI18nName);
            }

            for (int i = 0; i < sectorObjects.Count; i++)
            {
                sectorObjects[i].SetActive(selectedSectorIndex == i - 1);
            }
        }
    }
}
