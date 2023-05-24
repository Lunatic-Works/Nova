using System;
using UnityEngine;

namespace Nova
{
    public class GlobalShortcuts : MonoBehaviour
    {
        private InputManager inputManager;
        private RenderManager renderManager;

        private void Awake()
        {
            inputManager = Utils.FindNovaController().InputManager;
            renderManager = Utils.FindRenderManager();

            I18n.LocaleChanged.AddListener(OnLocaleChanged);
        }

        private void OnDestroy()
        {
            I18n.LocaleChanged.RemoveListener(OnLocaleChanged);
        }

        private void Update()
        {
            if (inputManager.IsTriggered(AbstractKey.ToggleFullScreen))
            {
                renderManager.SwitchFullScreen();
            }

            if (inputManager.IsTriggered(AbstractKey.SwitchLanguage))
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

        private static void OnLocaleChanged()
        {
            NovaAnimation.StopAll(AnimationType.Text);
        }
    }
}
