using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    // Attach this to provide text content according to the translation key.
    public class I18nText : MonoBehaviour
    {
        public string inflateTextKey;

        private Text text;
        private TMP_Text textPro;
        private TextProxy textProxy;

        private void Awake()
        {
            text = GetComponent<Text>();
            textPro = GetComponent<TMP_Text>();
            textProxy = GetComponent<TextProxy>();
            this.RuntimeAssert(text != null || textPro != null || textProxy != null,
                "Missing Text or TMP_Text or TextProxy.");
        }

        private void UpdateText()
        {
            // TODO: sometimes I18n may not have been initialized when the program just starts
            string str;
            try
            {
                str = I18n.__(inflateTextKey);
            }
            catch (KeyNotFoundException e)
            {
                Debug.Log(e);
                Debug.Log($"inflateTextKey {inflateTextKey}");
                str = inflateTextKey;
            }

            if (textProxy != null)
            {
                textProxy.Init();
                textProxy.text = str;
            }
            else if (textPro != null)
            {
                textPro.text = str;
            }
            else
            {
                text.text = str;
            }
        }

        private void OnEnable()
        {
            UpdateText();
            I18n.LocaleChanged.AddListener(UpdateText);
        }

        private void OnDisable()
        {
            I18n.LocaleChanged.RemoveListener(UpdateText);
        }
    }
}