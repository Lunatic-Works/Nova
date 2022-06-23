using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nova
{
    /// <summary>
    /// Handles the mapping among <see cref="AbstractKey"/>, <see cref="AbstractKeyGroup"/>, <see cref="InputAction"/>, and <see cref="InputActionMap"/>.<br/>
    /// Due to the implementation of action maps, abstract keys cannot belong to multiple abstract key groups.<br/>
    /// The input action asset is expected to match exactly with the abstract key (groups):
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// The action map names must match the abstract key group names.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// The action names must match the abstract key names.
    /// </description>
    /// </item>
    /// </list>
    /// </summary>
    public class ActionAssetData
    {
        public readonly InputActionAsset data;

        private readonly Dictionary<AbstractKey, AbstractKeyGroup> actionGroupsDic
            = new Dictionary<AbstractKey, AbstractKeyGroup>();

        private readonly Dictionary<AbstractKey, InputAction> actionsDic
            = new Dictionary<AbstractKey, InputAction>();

        private readonly Dictionary<AbstractKeyGroup, InputActionMap> actionMapsDic
            = new Dictionary<AbstractKeyGroup, InputActionMap>();

        /// <summary>
        /// Handles the mapping between abstract keys and action names.<br/>
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

            foreach (var key in actionAsset.actionMaps)
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

        public IEnumerable<InputActionMap> GetActionMaps(AbstractKeyGroup group)
            => actionMapsDic.Where(pair => group.HasFlag(pair.Key)).Select(pair => pair.Value);

        public InputActionMap GetActionMap(AbstractKeyGroup group)
            => actionMapsDic[group];

        public bool TryGetActionMap(AbstractKeyGroup group, out InputActionMap actionMap)
            => actionMapsDic.TryGetValue(group, out actionMap);

        public InputAction GetAction(AbstractKey key)
            => actionsDic[key];

        public bool TryGetAction(AbstractKey key, out InputAction action)
            => actionsDic.TryGetValue(key, out action);

        public AbstractKeyGroup GetActionGroup(AbstractKey key)
            => actionGroupsDic[key];

        public bool TryGetActionGroup(AbstractKey key, out AbstractKeyGroup group)
            => actionGroupsDic.TryGetValue(key, out group);

        public bool KeyIsEditor(AbstractKey key)
            => key.ToString().ToLower().StartsWith("editor", StringComparison.Ordinal);

        public ActionAssetData Clone()
            => new ActionAssetData(InputActionAsset.FromJson(data.ToJson()));
    }
}