using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nova
{
    using KeyStatus = Dictionary<AbstractKey, bool>;

    public class InputManager : MonoBehaviour
    {
        public static string InputFilesDirectory => Path.Combine(Application.persistentDataPath, "Input");
        private static string BindingsFilePath => Path.Combine(InputFilesDirectory, "bindings.json");

        public InputActionAsset defaultActionAsset;
        public ActionAssetData actionAsset { get; private set; }

        private void Load()
        {
            if (File.Exists(BindingsFilePath))
            {
                var json = File.ReadAllText(BindingsFilePath);
                actionAsset = new ActionAssetData(InputActionAsset.FromJson(json));
            }
        }

        public void Save()
        {
            var json = actionAsset.data.ToJson();
            Directory.CreateDirectory(InputFilesDirectory);
            File.WriteAllText(BindingsFilePath, json);
        }

        private void Awake()
        {
            Init();
            EnableInput();
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

            actionAsset = new ActionAssetData(defaultActionAsset.Clone());
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
                Debug.LogError($"Nova: Missing action key: {key}");
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
        /// Checks whether an abstract key is triggered.
        /// Only activates once. To check whether a key is held, use <see cref="IsPressed"/>.
        /// </summary>
        public bool IsTriggered(AbstractKey key)
        {
#if !UNITY_EDITOR
            if (ActionAssetData.IsEditorOnly(key))
            {
                return false;
            }
#endif

            if (!actionAsset.TryGetAction(key, out var action))
            {
                Debug.LogError($"Nova: Missing action key: {key}");
                return false;
            }

            return action.triggered;
        }

        public bool IsPressed(AbstractKey key)
        {
#if !UNITY_EDITOR
            if (ActionAssetData.IsEditorOnly(key))
            {
                return false;
            }
#endif

            if (!actionAsset.TryGetAction(key, out var action))
            {
                Debug.LogError($"Nova: Missing action key: {key}");
                return false;
            }

            return action.IsPressed();
        }

        public void SetActionAsset(ActionAssetData other)
        {
            var enabledState = new KeyStatus();
            GetEnabledState(enabledState);
            actionAsset?.data.Disable();
            actionAsset = other.Clone();
            SetEnabledState(enabledState);
        }

        public void GetEnabledState(KeyStatus status)
        {
            foreach (AbstractKey key in Enum.GetValues(typeof(AbstractKey)))
            {
                if (actionAsset.TryGetAction(key, out var action))
                {
                    status[key] = action.enabled;
                }
            }
        }

        public void SetEnabledState(KeyStatus status)
        {
            foreach (var key in status.Keys)
            {
                SetEnable(key, status[key]);
            }
        }

        // inputEnabled is not in RestoreData, because the user cannot save when the input is disabled
        public bool inputEnabled { get; private set; }

        // Disable all abstract keys except StepForward
        public void DisableInput()
        {
            inputEnabled = false;
            SetEnableGroup(AbstractKeyGroup.None);
            SetEnable(AbstractKey.StepForward, true);
        }

        public void EnableInput()
        {
            inputEnabled = true;
            SetEnableGroup(AbstractKeyGroup.All);
        }
    }
}
