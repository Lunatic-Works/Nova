using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace Nova
{
    [RequireComponent(typeof(PlayerInput))]
    public class InputSystemManager : MonoBehaviour
    {
        public static string InputFilesDirectory => Path.Combine(Application.persistentDataPath, "Input");
        private static string BindingsFilePath => Path.Combine(InputFilesDirectory, "bindings.json");

        private PlayerInput playerInput;
        public ActionAssetData actionAsset { get; private set; }

        private void Load()
        {
            if (File.Exists(BindingsFilePath))
            {
                var json = File.ReadAllText(BindingsFilePath);
                playerInput.actions.LoadBindingOverridesFromJson(json);
            }
        }

        public void Save()
        {
            var json = playerInput.actions.SaveBindingOverridesAsJson();
            File.WriteAllText(BindingsFilePath, json);
        }

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            Init();
        }

        private void OnDestroy()
        {
            Save();
        }

        public void Init()
        {
            if (actionAsset != null) return;

            EnhancedTouchSupport.Enable();
            TouchSimulation.Enable();
            actionAsset = new ActionAssetData(playerInput.actions);
            Load();
        }

        public void SetEnableGroup(AbstractKeyGroup group)
        {
            foreach (var actionMap in actionAsset.GetActionMaps(group))
                actionMap.Enable();

            foreach (var actionMap in actionAsset.GetActionMaps(AbstractKeyGroup.All ^ group))
                actionMap.Disable();
        }

        public void SetEnable(AbstractKey key, bool enable)
        {
            if (!actionAsset.TryGetAction(key, out var action))
            {
                Debug.LogError($"Missing action key: {key}");
                return;
            }
            if (enable)
            {
                action.Enable();
            }
            else
            {
                action.Disable();
            }
        }

        public bool IsTriggered(AbstractKey key)
        {
            if (!actionAsset.TryGetAction(key, out var action))
            {
                Debug.LogError($"Missing action key: {key}");
                return false;
            }
            return action.triggered;
        }

        public bool KeyIsEditor(AbstractKey key)
            => actionAsset.KeyIsEditor(key);

        public ActionAssetData CloneActionAsset()
            => actionAsset.Clone();

        public void OverrideActionAsset(InputActionAsset asset)
            => actionAsset.data.LoadBindingOverridesFromJson(asset.ToJson());
    }
}