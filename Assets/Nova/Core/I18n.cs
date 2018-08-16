using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

namespace Nova
{
    // object can be string or string[]
    //
    // For the i-th(zero-based) string in the string[],
    // it will be used as the translation if the first argument provided to __ is == i
    //
    // Example:
    // "ivebeenthere": ["I've never been there", "I've been there once", "I've been there twice", "I've been there {0} times"]
    // __("ivebeenthere", 2) == "I've been there twice"
    // __("ivebeenthere", 4) == "I've been there 4 times"
    using TranslationBundle = Dictionary<string, object>;

    public class I18n
    {
        public static readonly I18n Instance = new I18n();

        /// <summary>
        /// Translation data with a fallback chain.
        /// </summary>
        private List<TranslationBundle> _translationDatas = new List<TranslationBundle>();

        /// <summary>
        /// Supported locales. The first one is also the default and final fallback for all languages.
        /// </summary>
        private readonly SystemLanguage[] _locales = { SystemLanguage.Chinese, SystemLanguage.English };

        /// <summary>
        /// Fallback locale definition. _fallbacks[NotFoundLanguage] = FallbackLanguage
        /// </summary>
        private readonly Dictionary<SystemLanguage, SystemLanguage> _fallbacks = new Dictionary<SystemLanguage, SystemLanguage>()
        {
            { SystemLanguage.ChineseSimplified, SystemLanguage.Chinese },
            { SystemLanguage.ChineseTraditional, SystemLanguage.Chinese }
        };
        private SystemLanguage _currentLocale = Application.systemLanguage;

        public SystemLanguage CurrentLocale
        {
            get { return _currentLocale; }
            set { _currentLocale = value; Init(); }
        }

        private const string LocalePath = "Locales/";

        private I18n()
        {
            Init();
        }

        private void ChainLoadLanguage(SystemLanguage locale)
        {
            try
            {
                if (_locales.Contains(locale))
                {
#if UNITY_EDITOR
                    var all = File.ReadAllText(EditorPathRoot + locale + ".json");
                    _translationDatas.Add(JsonConvert.DeserializeObject<TranslationBundle>(all));
#else
                    var all = Resources.Load(LocalePath + locale) as TextAsset;
                    _translationDatas.Add(JsonConvert.DeserializeObject<TranslationBundle>(all.text));
#endif
                }
                else
                    throw new Exception();
            }
            catch
            {
                Debug.LogWarning("Nova: Locale [" + _currentLocale + "] not found in supported list");
#if UNITY_EDITOR
                _translationDatas.Add(new TranslationBundle());
                EditorOnly_SaveTranslation();
#endif
            }
            SystemLanguage next;
            if (_fallbacks.TryGetValue(locale, out next))
                ChainLoadLanguage(next);
            else if (_locales[0] != locale)
                ChainLoadLanguage(_locales[0]);
        }

        private void Init()
        {
            _translationDatas.Clear();
            ChainLoadLanguage(CurrentLocale);
#if UNITY_EDITOR
            _lastWriteTime = File.GetLastWriteTime(EditorTranslationPath);
#endif
        }

        /// <summary>
        /// Get the translation specified by key and optionally deal with the plurals and format arguments. (Shorthand)<para />
        /// When using in Unity Editor, missing translation will be automatically added to the corrsponding json file.<para />
        /// Also, translation will be automatically reloaded if the JSON file is changed.
        /// </summary>
        /// <param name="key">Key to specify the translation</param>
        /// <param name="args">Arguments to provide to the translation as a format string.<para />
        /// The first argument will be used to determine the quantity if needed.</param>
        /// <returns>The translated string.</returns>
        public static string __(string key, params object[] args)
        {
            return Instance.Translate(key, args);
        }

        /// <summary>
        /// Get the translation specified by key and optionally deal with the plurals and format arguments.<para />
        /// When using in Unity Editor, missing translation will be automatically added to the corrsponding json file.<para />
        /// Also, translation will be automatically reloaded if the JSON file is changed.
        /// </summary>
        /// <param name="key">Key to specify the translation</param>
        /// <param name="args">Arguments to provide to the translation as a format string.<para />
        /// The first argument will be used to determine the quantity if needed.</param>
        /// <returns>The translated string.</returns>
        public string Translate(string key, params object[] args)
        {
#if UNITY_EDITOR
            EditorOnly_GetLatestTranslation();
#endif
            string translation = key;
            object raw;
            int i = 0, n = _translationDatas.Count;
            for (i = 0; i < n; i++)
            {
                if (_translationDatas[i].TryGetValue(key, out raw))
                {
                    if (raw is string)
                        translation = raw as string;
                    else if (raw is string[])
                    {
                        var formats = raw as string[];
                        if (formats.Length == 0)
                            Debug.LogWarning("Nova: Empty string list for: " + key);
                        else if (args.Length == 0)
                            translation = formats[0];
                        else
                        {
                            // Assuming the first argument will determine the quantity
                            object arg1 = args[0];
                            long quantity = -1;
                            if (arg1 is int || arg1 is short || arg1 is long)
                                quantity = (long)arg1;
                            else if (arg1 is string && !long.TryParse(arg1 as string, out quantity))
                                quantity = -1;
                            if (quantity != -1)
                                translation = formats[Math.Min(quantity, formats.Length - 1)];
                        }
                    }
                    if (args.Length > 0)
                        translation = string.Format(translation, args);
#if UNITY_EDITOR
                    if (translation == key)
                        continue;
#endif
                    break;
                }
                if (i == 0)
                    Debug.LogWarning("Nova: Missing translation for: " + key);
            }
#if UNITY_EDITOR
            if (translation == key)
            {
                // Translation is not found by all means
                _translationDatas[0].Add(key, key);
                EditorOnly_SaveTranslation();
            }
#endif
            return translation;
        }

#if UNITY_EDITOR
        private string EditorTranslationPath
        {
            get { return EditorPathRoot + _currentLocale + ".json"; }
        }

        private string EditorPathRoot
        {
            get { return "Assets/Nova/Resources/" + LocalePath; }
        }
        private DateTime _lastWriteTime;
        private void EditorOnly_SaveTranslation()
        {
            Directory.CreateDirectory(EditorPathRoot);
            File.WriteAllText(EditorTranslationPath, JsonConvert.SerializeObject(_translationDatas[0], Formatting.Indented));
            _lastWriteTime = File.GetLastWriteTime(EditorTranslationPath);
        }

        private void EditorOnly_GetLatestTranslation()
        {
            if (File.GetLastWriteTime(EditorTranslationPath) != _lastWriteTime)
                Init();
        }
#endif
    }
}