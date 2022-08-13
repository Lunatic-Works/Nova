using System;
using UnityEngine;

namespace Nova
{
    public class GlobalShortcuts : MonoBehaviour
    {
        private InputManager inputManager;

        private void Awake()
        {
            inputManager = Utils.FindNovaGameController().InputManager;
        }

        private void Update()
        {
            if (inputManager.IsTriggered(AbstractKey.ToggleFullScreen))
            {
                GameRenderManager.SwitchFullScreen();
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
    }
}
