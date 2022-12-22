using System;
using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Wrapper for finding all necessary components in Nova
    /// </summary>
    public class NovaController : MonoBehaviour
    {
        public GameState GameState { get; private set; }
        public DialogueState DialogueState { get; private set; }
        public CheckpointManager CheckpointManager { get; private set; }
        public ConfigManager ConfigManager { get; private set; }
        public InputManager InputManager { get; private set; }
        public CheckpointHelper CheckpointHelper { get; private set; }

        public NovaAnimation PerDialogueAnimation { get; private set; }
        public NovaAnimation HoldingAnimation { get; private set; }

        private void Awake()
        {
            GameState = FindComponent<GameState>();
            DialogueState = FindComponent<DialogueState>();
            CheckpointManager = FindComponent<CheckpointManager>();
            ConfigManager = FindComponent<ConfigManager>();
            InputManager = FindComponent<InputManager>();
            CheckpointHelper = FindComponent<CheckpointHelper>();

            var animations = GetComponentsInChildren<NovaAnimation>();
            PerDialogueAnimation = Array.Find(animations, x => x.type == AnimationType.PerDialogue);
            AssertNotNull(PerDialogueAnimation, "PerDialogueAnimation");
            HoldingAnimation = Array.Find(animations, x => x.type == AnimationType.Holding);
            AssertNotNull(HoldingAnimation, "HoldingAnimation");
        }

        private static void AssertNotNull(MonoBehaviour component, string name)
        {
            Utils.RuntimeAssert(component != null, $"Cannot find {name}, ill-formed NovaController game object.");
        }

        private T FindComponent<T>(string childPath = "") where T : MonoBehaviour
        {
            var go = gameObject;
            if (!string.IsNullOrEmpty(childPath))
            {
                go = transform.Find(childPath).gameObject;
            }

            var component = go.GetComponent<T>();
            AssertNotNull(component, typeof(T).Name);
            return component;
        }
    }
}
