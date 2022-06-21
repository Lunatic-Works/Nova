using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace Nova
{
    using KeyStatus = Dictionary<AbstractKey, bool>;

    [RequireComponent(typeof(PlayerInput))]
    public class InputSystemManager : MonoBehaviour
    {
        public static string InputFilesDirectory => Path.Combine(Application.persistentDataPath, "Input");
        private static string BindingsFilePath => Path.Combine(InputFilesDirectory, "bindings.json");

        private PlayerInput playerInput;
        public ActionAssetData actionAsset { get; private set; }
        public InputActionAsset defaultActionAsset { get; private set; }

        private void Load()
        {
            if (File.Exists(BindingsFilePath))
            {
                var json = File.ReadAllText(BindingsFilePath);
                playerInput.actions = InputActionAsset.FromJson(json);
                actionAsset = new ActionAssetData(playerInput.actions);
            }
        }

        public void Save()
        {
            var json = playerInput.actions.ToJson();
            if (!Directory.Exists(InputFilesDirectory))
            {
                Directory.CreateDirectory(InputFilesDirectory);
            }
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

        /// <summary>
        /// Must be called before accessing any other members.
        /// </summary>
        public void Init()
        {
            if (actionAsset != null) return;

            EnhancedTouchSupport.Enable();
            TouchSimulation.Enable();
            defaultActionAsset = InputActionAsset.FromJson(playerInput.actions.ToJson());
            playerInput.actions = InputActionAsset.FromJson(defaultActionAsset.ToJson());
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

        /// <summary>
        /// Checks whether an abstract key is triggered.<br/>
        /// Only activates once. To check whether a key is held, use <see cref="InputAction.IsPressed"/>.
        /// </summary>
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

        public void SetActionAsset(InputActionAsset asset)
        {
            playerInput.actions = InputActionAsset.FromJson(asset.ToJson());
            actionAsset = new ActionAssetData(playerInput.actions);
        }

        public KeyStatus GetEnabledState()
        {
            var status = new KeyStatus();
            foreach (var key in Enum.GetValues(typeof(AbstractKey)))
            {
                if (actionAsset.TryGetAction((AbstractKey)key, out var action))
                {
                    status.Add((AbstractKey)key, action.enabled);
                }
            }
            return status;
        }

        public void SetEnabledState(KeyStatus status)
        {
            foreach (var key in status.Keys)
            {
                SetEnable(key, status[key]);
            }
        }
    }
}