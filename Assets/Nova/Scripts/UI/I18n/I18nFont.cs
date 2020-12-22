using UnityEngine;

namespace Nova
{
    [RequireComponent(typeof(TextProxy))]
    public class I18nFont : MonoBehaviour
    {
        private TextProxy textProxy;

        private void Awake()
        {
            textProxy = GetComponent<TextProxy>();
        }

        private void OnEnable()
        {
            textProxy.Init();
            textProxy.UpdateFont();
            I18n.LocaleChanged.AddListener(textProxy.UpdateFont);
        }

        private void OnDisable()
        {
            I18n.LocaleChanged.RemoveListener(textProxy.UpdateFont);
        }
    }
}