using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Nova
{
    /// <summary>
    /// On Windows, there is a virtual mouse driven by the touch, so we disable mouse for a short time after each touch
    /// </summary>
    public class TouchFix : MonoBehaviour
    {
        private const float MaxIdleTime = 0.2f;

        public static bool UsingTouch;

        public static bool IsTouch(ExtendedPointerEventData eventData)
        {
            return UsingTouch || eventData.pointerType == UIPointerType.Touch;
        }

        private bool inited;
        private float idleTime;

        // If we call it in Start, EventSystem.current can be uninitialized
        private void Init()
        {
            if (inited)
            {
                return;
            }

            EnhancedTouchSupport.Enable();
            Touch.onFingerDown += OnFingerDown;

            // Multi-touch is disabled
            var inputModule = (InputSystemUIInputModule)EventSystem.current.currentInputModule;
            inputModule.pointerBehavior = UIPointerBehavior.SingleUnifiedPointer;

            inited = true;
        }

        private void OnDestroy()
        {
            Touch.onFingerDown -= OnFingerDown;
        }

        private void OnFingerDown(Finger finger)
        {
            if (!UsingTouch)
            {
                UsingTouch = true;
                idleTime = 0.0f;
            }
        }

        private void Update()
        {
            // Currently we assume that mobile platform = full screen, touch screen, no mouse/keyboard/gamepad
            // We may properly detect those features in future
            if (Application.isMobilePlatform || Touchscreen.current == null)
            {
                enabled = false;
                return;
            }

            Init();

            if (Touch.activeTouches.Count == 0)
            {
                if (UsingTouch)
                {
                    idleTime += Time.unscaledDeltaTime;
                    if (idleTime > MaxIdleTime)
                    {
                        UsingTouch = false;
                    }
                }
            }
        }
    }
}
