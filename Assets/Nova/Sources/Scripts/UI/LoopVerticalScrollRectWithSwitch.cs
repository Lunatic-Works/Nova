using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nova
{
    // Used to disable scrolling when log view just showed
    public class LoopVerticalScrollRectWithSwitch : LoopVerticalScrollRect
    {
        public bool scrollable = true;

        public override void OnScroll(PointerEventData data)
        {
            if (scrollable)
            {
                base.OnScroll(data);
            }
        }

        protected override float GetSize(RectTransform item, bool includeSpacing)
        {
            var logEntry = item.GetComponent<LogEntryController>();
            if (logEntry != null)
            {
                return logEntry.height + (includeSpacing ? contentSpacing : 0);
            }

            return base.GetSize(item, includeSpacing);
        }
    }
}
