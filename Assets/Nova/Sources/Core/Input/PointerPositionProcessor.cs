using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nova
{
    /// <summary>
    /// Adds offsets to pointer coordinates in case the screen is scaled due to unexpected aspect ratio.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class PointerPositionProcessor : InputProcessor<Vector2>
    {
#if UNITY_EDITOR
        static PointerPositionProcessor()
        {
            Initialize();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            InputSystem.RegisterProcessor<PointerPositionProcessor>();
        }

        public override Vector2 Process(Vector2 value, InputControl control)
        {
            return Cursor.visible ? value - RealScreen.offset : Vector2.positiveInfinity;
        }
    }
}