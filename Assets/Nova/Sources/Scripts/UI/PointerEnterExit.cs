using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace Nova
{
    public class PointerEnterExit : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public UnityEvent onPointerEnter;
        public UnityEvent onPointerExit;

        public void OnPointerEnter(PointerEventData eventData)
        {
            onPointerEnter.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            onPointerExit.Invoke();
        }
    }
}