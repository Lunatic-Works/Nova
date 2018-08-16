using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    [RequireComponent(typeof(SpriteChangerWithFade))]
    public class ButtonSpriteController : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private SpriteChangerWithFade _spriteChanger;

        public Sprite normalSprite;
        public Sprite onHoverSprite;
        public Sprite onPressedSprite;

        private void Awake()
        {
            _spriteChanger = GetComponent<SpriteChangerWithFade>();
        }

        private bool pointerIsInside;

        public void OnPointerDown(PointerEventData eventData)
        {
            _spriteChanger.sprite = onPressedSprite;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (pointerIsInside)
            {
                _spriteChanger.sprite = onHoverSprite;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerIsInside = true;
            _spriteChanger.sprite = onHoverSprite;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerIsInside = false;
            _spriteChanger.sprite = normalSprite;
        }
    }
}