using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nova
{
    /// <summary>
    /// Handles the mapping among <see cref="AbstractKey"/>, <see cref="AbstractKeyGroup"/>, <see cref="InputAction"/>, and <see cref="InputActionMap"/>.
    /// Due to the implementation of action maps, abstract keys cannot belong to multiple abstract key groups.
    /// The input action asset is expected to match exactly with the abstract key (groups):
    /// * The action map names must match the abstract key group names.
    /// * The action names must match the abstract key names.
    /// </summary>
    public class ActionAssetData
    {
        public readonly InputActionAsset data;

        private readonly Dictionary<AbstractKey, AbstractKeyGroup> actionGroups
            = new Dictionary<AbstractKey, AbstractKeyGroup>();

        private readonly Dictionary<AbstractKey, InputAction> actions
            = new Dictionary<AbstractKey, InputAction>();

        private readonly Dictionary<AbstractKeyGroup, InputActionMap> actionMaps
            = new Dictionary<AbstractKeyGroup, InputActionMap>();

        /// <summary>
        /// Handles the mapping between abstract keys and action names.
        /// Usually just parses enum names. Exceptions can be added here.
        /// </summary>
        /// <returns>Whether the translation is successful</returns>
        private static bool ActionNameToKey(string actionName, out AbstractKey key)
            => Enum.TryParse(actionName, out key);

        public ActionAssetData(InputActionAsset actionAsset)
        {
            data = actionAsset;

            foreach (var action in actionAsset)
            {
                if (!ActionNameToKey(action.name, out var key))
                {
                    Debug.LogError($"Nova: Unknown action name: {action.name}");
                }
                else if (actionGroups.ContainsKey(key))
                {
                    Debug.LogError($"Nova: Duplicate action key: {action.name}");
                }
                else if (!Enum.TryParse(action.actionMap.name, out AbstractKeyGroup group))
                {
                    Debug.LogError($"Nova: Unknown action group: {action.actionMap.name}");
                }
                else
                {
                    actionGroups[key] = group;
                    actions[key] = action;
                }
            }

            foreach (AbstractKey key in Enum.GetValues(typeof(AbstractKey)))
            {
                if (!actions.ContainsKey(key))
                {
                    Debug.LogError($"Nova: Missing action key: {key}");
                }
            }

            foreach (var key in actionAsset.actionMaps)
            {
                if (!Enum.TryParse(key.name, out AbstractKeyGroup group))
                {
                    Debug.LogError($"Nova: Unknown action group: {key.name}");
                }
                else
                {
                    actionMaps[group] = key;
                }
            }
        }

        public IEnumerable<InputActionMap> GetActionMaps(AbstractKeyGroup group)
            => actionMaps.Where(pair => group.HasFlag(pair.Key)).Select(pair => pair.Value);

        public InputActionMap GetActionMap(AbstractKeyGroup group)
            => actionMaps[group];

        public bool TryGetActionMap(AbstractKeyGroup group, out InputActionMap actionMap)
            => actionMaps.TryGetValue(group, out actionMap);

        public InputAction GetAction(AbstractKey key)
            => actions[key];

        public bool TryGetAction(AbstractKey key, out InputAction action)
            => actions.TryGetValue(key, out action);

        public AbstractKeyGroup GetActionGroup(AbstractKey key)
            => actionGroups[key];

        public bool TryGetActionGroup(AbstractKey key, out AbstractKeyGroup group)
            => actionGroups.TryGetValue(key, out group);

        public static bool IsEditorOnly(AbstractKey key)
            => key.ToString().ToLower().StartsWith("editor", StringComparison.Ordinal);

        public ActionAssetData Clone()
            => new ActionAssetData(data.Clone());
    }

    public static class InputActionAssetExtensions
    {
        public static InputActionAsset Clone(this InputActionAsset asset)
        {
            return InputActionAsset.FromJson(asset.ToJson());
        }
    }
}
