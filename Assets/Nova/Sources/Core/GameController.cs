using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Wrapper for finding all necessary components in NovaGameController
    /// </summary>
    public class GameController : MonoBehaviour
    {
        public GameState GameState { get; private set; }
        public CheckpointManager CheckpointManager { get; private set; }
        public ConfigManager ConfigManager { get; private set; }
        public InputMapper InputMapper { get; private set; }
        public AssetLoader AssetLoader { get; private set; }
        public CheckpointHelper CheckpointHelper { get; private set; }
        public InputHelper InputHelper { get; private set; }
        public NovaAnimation PersistAnimation { get; private set; }
        public NovaAnimation PerDialogueAnimation { get; private set; }

        private void Awake()
        {
            GameState = FindComponent<GameState>();
            CheckpointManager = FindComponent<CheckpointManager>();
            ConfigManager = FindComponent<ConfigManager>();
            InputMapper = FindComponent<InputMapper>();
            AssetLoader = FindComponent<AssetLoader>();
            CheckpointHelper = FindComponent<CheckpointHelper>();
            InputHelper = FindComponent<InputHelper>();
            PerDialogueAnimation = FindComponent<NovaAnimation>("NovaAnimation/PerDialogue");
            PersistAnimation = FindComponent<NovaAnimation>("NovaAnimation/Persistent");

            inputEnabled = true;
        }

        private static T AssertNotNull<T>(T component, string name) where T : MonoBehaviour
        {
            Utils.RuntimeAssert(component != null, $"Cannot find {name}, ill-formed NovaGameController.");
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

        // inputEnabled is not in RestoreData, because the user cannot save when the input is disabled
        public bool inputEnabled { get; private set; }

        // Disable all abstract keys except StepForward
        public void DisableInput()
        {
            inputEnabled = false;
            InputMapper.SetEnableGroup(AbstractKeyGroup.None);
            InputMapper.SetEnable(AbstractKey.StepForward, true);
        }

        public void EnableInput()
        {
            inputEnabled = true;
            InputMapper.SetEnableGroup(AbstractKeyGroup.Game | AbstractKeyGroup.UI);
        }
    }
}