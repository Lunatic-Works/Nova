using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    public class ConfigManager : MonoBehaviour
    {
        public const string TrackedKeysKey = "TrackedKeys";
        public const string TrackedKeyPrefix = "_";
        public const string FirstShownKeyPrefix = TrackedKeyPrefix + "FirstShown";

        public class SettingDefinition
        {
            public float? min;
            public float? max;
            public bool? whole;
            public string defaultValue;
        }

        public TextAsset defaultSettingsJson;

        private Dictionary<string, SettingDefinition> definitions;

        private void Awake()
        {
            if (defaultSettingsJson == null)
            {
                definitions = new Dictionary<string, SettingDefinition>();
                return;
            }

            definitions =
                JsonConvert.DeserializeObject<Dictionary<string, SettingDefinition>>(defaultSettingsJson.text);
        }

        private void OnDestroy()
        {
            Apply();
        }

        public void Apply()
        {
            foreach (var entry in tmpStrCache)
            {
                PlayerPrefs.SetString(entry.Key, entry.Value);
            }

            foreach (var entry in tmpFloatCache)
            {
                PlayerPrefs.SetString(entry.Key, $"{entry.Value:0.###}");
            }

            foreach (var entry in tmpIntCache)
            {
                PlayerPrefs.SetString(entry.Key, $"{entry.Value}");
            }

            ClearCache();

            PlayerPrefs.Save();
        }

        private void ClearCache()
        {
            tmpStrCache.Clear();
            tmpFloatCache.Clear();
            tmpIntCache.Clear();
        }

        public void Restore()
        {
            ClearCache();
            NotifyAll();
        }

        public void ResetDefault()
        {
            ClearCache();
            PlayerPrefs.DeleteAll();
            NotifyAll();
        }

        public SettingDefinition GetDefinition(string key)
        {
            definitions.TryGetValue(key, out var result);
            return result;
        }

        // All get methods first search tmp cache and then player prefs and then default values
        public string GetString(string key, string defaultValue = "")
        {
            if (tmpStrCache.TryGetValue(key, out var value))
            {
                return value;
            }

            if (PlayerPrefs.HasKey(key))
            {
                return PlayerPrefs.GetString(key, defaultValue);
            }

            if (definitions.TryGetValue(key, out var def))
            {
                return def.defaultValue;
            }

            if (key.StartsWith(Alert.AlertKeyPrefix, StringComparison.Ordinal))
            {
                return "1";
            }

            return defaultValue;
        }

        public float GetFloat(string key, float defaultValue = 0)
        {
            if (tmpFloatCache.TryGetValue(key, out var value))
            {
                return value;
            }

            var str = GetString(key, null);
            if (str == null)
            {
                return defaultValue;
            }

            return !float.TryParse(str, out value) ? defaultValue : value;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            if (tmpIntCache.TryGetValue(key, out var value))
            {
                return value;
            }

            var str = GetString(key, null);
            if (str == null)
            {
                return defaultValue;
            }

            return !int.TryParse(str, out value) ? defaultValue : value;
        }

        private readonly Dictionary<string, string> tmpStrCache = new Dictionary<string, string>();
        private readonly Dictionary<string, int> tmpIntCache = new Dictionary<string, int>();
        private readonly Dictionary<string, float> tmpFloatCache = new Dictionary<string, float>();

        private void TryTrack(string key)
        {
            if (key.StartsWith(TrackedKeyPrefix, StringComparison.Ordinal))
            {
                var trackedKeys = new SortedSet<string>(GetAllTrackedKeys()) {key};
                SetString(TrackedKeysKey, string.Join(",", trackedKeys));
            }
        }

        public void SetString(string key, string value)
        {
            TryTrack(key);
            tmpStrCache[key] = value;
            Notify(key);
        }

        public void SetInt(string key, int value)
        {
            TryTrack(key);
            tmpIntCache[key] = value;
            Notify(key);
        }

        public void SetFloat(string key, float value)
        {
            TryTrack(key);
            tmpFloatCache[key] = value;
            Notify(key);
        }

        private readonly Dictionary<string, Action> valueChangeListeners = new Dictionary<string, Action>();

        public void AddValueChangeListener(string key, Action onValueChange)
        {
            if (valueChangeListeners.ContainsKey(key))
            {
                valueChangeListeners[key] += onValueChange;
                return;
            }

            valueChangeListeners.Add(key, onValueChange);
        }

        public void RemoveValueChangeListener(string key, Action onValueChange)
        {
            if (!valueChangeListeners.ContainsKey(key)) return;
            if (onValueChange != null) valueChangeListeners[key] -= onValueChange;
        }

        private void Notify(string key)
        {
            if (valueChangeListeners.TryGetValue(key, out var action))
            {
                action?.Invoke();
            }
        }

        private void NotifyAll()
        {
            foreach (var entry in valueChangeListeners)
            {
                entry.Value?.Invoke();
            }
        }

        public IEnumerable<string> GetAllTrackedKeys()
        {
            return GetString(TrackedKeysKey).Split(new[] {','}, System.StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
