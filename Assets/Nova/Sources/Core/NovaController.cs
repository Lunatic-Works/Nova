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
        public NovaAnimation UIAnimation { get; private set; }
        public NovaAnimation TextAnimation { get; private set; }

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
            HoldingAnimation = Array.Find(animations, x => x.type == AnimationType.Holding);
            UIAnimation = Array.Find(animations, x => x.type == AnimationType.UI);
            TextAnimation = Array.Find(animations, x => x.type == AnimationType.Text);
            AssertNotNull(PerDialogueAnimation, "PerDialogueAnimation");
            AssertNotNull(HoldingAnimation, "HoldingAnimation");
            AssertNotNull(UIAnimation, "UIAnimation");
            AssertNotNull(TextAnimation, "TextAnimation");
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
