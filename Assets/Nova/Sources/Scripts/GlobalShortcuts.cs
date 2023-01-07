using System;
using UnityEngine;

namespace Nova
{
    public class GlobalShortcuts : MonoBehaviour
    {
        private InputMapper inputMapper;

        private void Awake()
        {
            inputMapper = Utils.FindNovaController().InputMapper;

            I18n.LocaleChanged.AddListener(OnLocaleChanged);
        }

        private void OnDestroy()
        {
            I18n.LocaleChanged.RemoveListener(OnLocaleChanged);
        }

        private void Update()
        {
            if (inputMapper.GetKeyUp(AbstractKey.ToggleFullScreen))
            {
                RenderManager.SwitchFullScreen();
            }

            if (inputMapper.GetKeyUp(AbstractKey.SwitchLanguage))
            {
                SwitchLanguage();
            }
        }

        private static void SwitchLanguage()
        {
            var i = Array.IndexOf(I18n.SupportedLocales, I18n.CurrentLocale);
            i = (i + 1) % I18n.SupportedLocales.Length;
            I18n.CurrentLocale = I18n.SupportedLocales[i];
        }

        private void OnLocaleChanged()
        {
            NovaAnimation.StopAll(AnimationType.Text);
        }
    }
}
