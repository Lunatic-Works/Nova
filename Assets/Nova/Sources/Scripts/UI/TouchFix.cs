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

        private bool inited;
        private InputAction pointAction;
        private InputAction clickAction;

        private bool usingTouch;
        private float idleTime;

        // If we call it in Start, EventSystem.current can be uninitialized
        private void Init()
        {
            if (inited)
            {
                return;
            }

            EnhancedTouchSupport.Enable();

            var inputModule = (InputSystemUIInputModule)EventSystem.current.currentInputModule;
            var actions = inputModule.actionsAsset;
            pointAction = actions.FindAction("Point", true);
            clickAction = actions.FindAction("Click", true);

            inited = true;
        }

        private void Update()
        {
            if (Application.isMobilePlatform)
            {
                return;
            }

            Init();

            if (Touch.activeTouches.Count > 0)
            {
                if (!usingTouch)
                {
                    usingTouch = true;
                    idleTime = 0.0f;
                    pointAction.ChangeBindingWithPath("<Mouse>/position").Erase();
                    clickAction.ChangeBindingWithPath("<Mouse>/leftButton").Erase();
                }
            }
            else
            {
                if (usingTouch)
                {
                    idleTime += Time.unscaledDeltaTime;
                    if (idleTime > MaxIdleTime)
                    {
                        usingTouch = false;
                        pointAction.AddBinding("<Mouse>/position");
                        clickAction.AddBinding("<Mouse>/leftButton");
                    }
                }
            }
        }
    }
}
