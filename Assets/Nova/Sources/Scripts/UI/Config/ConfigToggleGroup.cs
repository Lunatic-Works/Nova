using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [AttributeUsage(AttributeTargets.Field)]
    public class EnumDisplayNameAttribute : Attribute
    {
        public readonly string displayName;

        public EnumDisplayNameAttribute(string displayName)
        {
            this.displayName = displayName;
        }
    }

    /// <summary>
    /// Use toggle group to modify the value in ConfigManager
    /// </summary>
    [RequireComponent(typeof(ToggleGroup))]
    public class ConfigToggleGroup : MonoBehaviour
    {
        [SerializeField] private string configKeyName;
        [SerializeField] private string enumTypeName;
        [SerializeField] private GameObject togglePrefab;

        private ToggleGroup toggleGroup;
        private List<Toggle> toggles;
        private ConfigManager configManager;

        private void Awake()
        {
            this.RuntimeAssert(!string.IsNullOrEmpty(configKeyName), "Empty configKeyName.");

            toggleGroup = GetComponent<ToggleGroup>();
            configManager = Utils.FindNovaController().ConfigManager;

            var configEnum = Type.GetType("Nova." + enumTypeName);
            if (configEnum != null && configEnum.IsEnum)
            {
                toggles = Enum.GetNames(configEnum).Select(name =>
                {
                    var attrs = configEnum.GetMember(name)[0]
                        .GetCustomAttributes(typeof(EnumDisplayNameAttribute), false);
                    var displayName = attrs.Length > 0 ? (attrs[0] as EnumDisplayNameAttribute).displayName : name;
                    var go = Instantiate(togglePrefab, transform);
                    go.GetComponentInChildren<Text>().text = displayName;
                    var toggle = go.GetComponentInChildren<Toggle>();
                    toggle.group = toggleGroup;
                    return toggle;
                }).ToList();
                toggleGroup.SetAllTogglesOff();
            }
        }

        private void UpdateValue()
        {
            var value = configManager.GetInt(configKeyName);
            if (toggles.Count > value && value >= 0 && toggles[value].isOn)
            {
                // Eliminate infinite recursion
                return;
            }

            toggles[value].isOn = true;
        }

        private void OnValueChange(bool _)
        {
            for (int i = 0; i < toggles.Count; i++)
            {
                if (toggles[i].isOn)
                {
                    configManager.SetInt(configKeyName, i);
                    return;
                }
            }
        }

        private void OnEnable()
        {
            foreach (var toggle in toggles)
            {
                toggle.onValueChanged.AddListener(OnValueChange);
            }

            configManager.AddValueChangeListener(configKeyName, UpdateValue);
            UpdateValue();
        }

        private void OnDisable()
        {
            foreach (var toggle in toggles)
            {
                toggle.onValueChanged.RemoveListener(OnValueChange);
            }

            configManager.RemoveValueChangeListener(configKeyName, UpdateValue);
        }
    }
}
