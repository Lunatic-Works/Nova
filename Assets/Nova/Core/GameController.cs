using UnityEngine;
using UnityEngine.Assertions;

namespace Nova
{
    /// <summary>
    /// Wrapper for finding all necessary components in NovaGameController
    /// </summary>
    public class GameController : MonoBehaviour
    {
        public GameState GameState { get; private set; }
        public CheckpointManager CheckpointManager { get; private set; }
        public FancyCursor FancyCursor { get; private set; }
        public ConfigManager ConfigManager { get; private set; }
        public InputMapper InputMapper { get; private set; }
        public NovaAnimation PersistAnimation { get; private set; }
        public NovaAnimation PerDialogueAnimation { get; private set; }

        // private DummyInputSystem _dummyInput;
        // private BaseInput _previousInputOverride;

        private bool _disableInput = false;

        // DisableInput is not in RestoreData, because the user cannot save when
        // the input is disabled
        public bool disableInput
        {
            get => _disableInput;
            set
            {
                _disableInput = value;
                InputMapper.SetEnableAll(!value);
                // Very ugly here. This method currently only used for chap 15 of COI, where
                // all abstract keys should be disabled, except StepForward
                InputMapper.SetEnable(AbstractKey.StepForward, true);

                // var current = EventSystem.current;
                // var currentInputModule = current.currentInputModule;
                // if (value && currentInputModule.inputOverride != _dummyInput)
                // {
                //     _previousInputOverride =
                //         currentInputModule.inputOverride;
                //     currentInputModule.inputOverride = _dummyInput;
                // }
                // else
                // {
                //     currentInputModule.inputOverride = _previousInputOverride;
                // }
            }
        }

        private void Awake()
        {
            GameState = FindComponent<GameState>();
            CheckpointManager = FindComponent<CheckpointManager>();
            FancyCursor = FindComponent<FancyCursor>();
            ConfigManager = FindComponent<ConfigManager>();
            InputMapper = FindComponent<InputMapper>();
            PerDialogueAnimation = FindComponent<NovaAnimation>("NovaAnimation/PerDialogue");
            PersistAnimation = FindComponent<NovaAnimation>("NovaAnimation/Persistent");
            // _dummyInput = FindComponent<DummyInputSystem>();
        }

        private static T AssertNotNull<T>(T component, string name) where T : MonoBehaviour
        {
            Assert.IsNotNull(component, $"Nova: Cannot find {name}, ill formed NovaGameController.");
            return component;
        }

        private T FindComponent<T>(string childPath = "") where T : MonoBehaviour
        {
            var go = gameObject;
            if (!string.IsNullOrEmpty(childPath))
            {
                go = transform.Find(childPath).gameObject;
            }

            var cmp = go.GetComponent<T>();
            return AssertNotNull(cmp, childPath + "/" + typeof(T).Name);
        }
    }
}