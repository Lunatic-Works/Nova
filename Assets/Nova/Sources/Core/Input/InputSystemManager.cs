using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nova
{
    [RequireComponent(typeof(PlayerInput))]
    public class InputSystemManager : MonoBehaviour
    {
        public static string InputFilesDirectory => Path.Combine(Application.persistentDataPath, "Input");
        private static string BindingsFilePath => Path.Combine(InputFilesDirectory, "bindings.json");

        private PlayerInput playerInput;
        private readonly Dictionary<AbstractKey, AbstractKeyGroup> actionGroupsDic = new();
        private readonly Dictionary<AbstractKey, InputAction> actionsDic = new();
        private readonly Dictionary<AbstractKeyGroup, InputActionMap> actionMapsDic = new();

        private void LoadBindings()
        {
            if (File.Exists(BindingsFilePath))
            {
                var json = File.ReadAllText(BindingsFilePath);
                playerInput.actions.LoadBindingOverridesFromJson(json);
            }
        }

        private void SaveBindings()
        {
            var json = playerInput.actions.SaveBindingOverridesAsJson();
            File.WriteAllText(BindingsFilePath, json);
        }

        private string GetActionPath(AbstractKey key)
        {
            var group = actionGroupsDic[key];
            return $"{group}/{key}";
        }

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
        }

        private void OnDestroy()
        {
            SaveBindings();
        }

        public void Init()
        {
            LoadBindings();
            actionGroupsDic.Clear();
            foreach (var action in playerInput.actions)
            {
                if (!Enum.TryParse(action.name, out AbstractKey key))
                {
                    Debug.LogError($"Unknown action name: {action.name}");
                }
                else if (actionGroupsDic.ContainsKey(key))
                {
                    Debug.LogError($"Duplicate action key: {action.name}");
                }
                else if (!Enum.TryParse(action.actionMap.name, out AbstractKeyGroup group))
                {
                    Debug.LogError($"Unknown action group: {action.actionMap.name}");
                }
                else
                {
                    actionGroupsDic[key] = group;
                    actionsDic[key] = action;
                }
            }
            foreach (var key in Enum.GetValues(typeof(AbstractKey)))
            {
                if (!actionsDic.ContainsKey((AbstractKey)key))
                {
                    Debug.LogError($"Missing action key: {key}");
                }
            }
            foreach (var key in playerInput.actions.actionMaps)
            {
                if (!Enum.TryParse(key.name, out AbstractKeyGroup group))
                {
                    Debug.LogError($"Unknown action group: {key.name}");
                }
                else
                {
                    actionMapsDic[group] = key;
                }
            }
        }

        public void SetEnableGroup(AbstractKeyGroup group)
        {
            for (int i = 0; i < playerInput.actions.actionMaps.Count; i++)
            {
                var map = playerInput.actions.actionMaps[i];
                if ((((int)group >> i) & 1) > 0)
                {
                    map.Enable();
                }
                else
                {
                    map.Disable();
                }
            }
        }

        public InputActionMap GetActionMap(AbstractKeyGroup group)
        {
            return actionMapsDic[group];
        }

        public InputAction GetAction(AbstractKey key)
        {
            return actionsDic[key];
        }
    }
}