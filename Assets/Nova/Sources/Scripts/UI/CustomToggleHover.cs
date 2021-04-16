using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nova
{
    [RequireComponent(typeof(Toggle))]
    public class CustomToggleHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Sprite inactiveHoverSprite;
        public Sprite activeHoverSprite;

        private Image back, fore;
        private Sprite inactiveSprite;
        private Sprite activeSprite;

        private void Awake()
        {
            var toggle = GetComponent<Toggle>();
            back = toggle.targetGraphic as Image;
            fore = toggle.graphic as Image;
            this.RuntimeAssert(back != null && fore != null, "Graphic should be Image.");
            inactiveSprite = back.sprite;
            activeSprite = fore.sprite;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            fore.sprite = activeHoverSprite;
            fore.SetNativeSize();
            back.sprite = inactiveHoverSprite;
            back.SetNativeSize();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            fore.sprite = activeSprite;
            fore.SetNativeSize();
            back.sprite = inactiveSprite;
            back.SetNativeSize();
        }
    }
}