// DEPRECATED, please use CursorManager instead
// https://answers.unity.com/questions/1313950/unity-ui-mouse-keyboard-navigate-un-highlight-butt.html

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nova
{
    [RequireComponent(typeof(Selectable))]
    public class HighlightFix : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Selectable selectable;

        private void Awake()
        {
            selectable = GetComponent<Selectable>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // var eventSystem = EventSystem.current;
            // if (!eventSystem.alreadySelecting)
            // {
            //     eventSystem.SetSelectedGameObject(gameObject);
            // }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // selectable.OnDeselect(eventData as BaseEventData);
        }
    }
}