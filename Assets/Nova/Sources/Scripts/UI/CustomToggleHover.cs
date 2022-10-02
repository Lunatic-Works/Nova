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

        private Image back;
        private Image fore;
        private Sprite inactiveSprite;
        private Sprite activeSprite;

        private void Awake()
        {
            var toggle = GetComponent<Toggle>();
            back = toggle.targetGraphic as Image;
            fore = toggle.graphic as Image;
            this.RuntimeAssert(back != null, "toggle.targetGraphic should be Image.");
            this.RuntimeAssert(fore != null, "toggle.graphic should be Image.");
            inactiveSprite = back.sprite;
            activeSprite = fore.sprite;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            back.sprite = inactiveHoverSprite;
            back.SetNativeSize();
            fore.sprite = activeHoverSprite;
            fore.SetNativeSize();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            back.sprite = inactiveSprite;
            back.SetNativeSize();
            fore.sprite = activeSprite;
            fore.SetNativeSize();
        }
    }
}
