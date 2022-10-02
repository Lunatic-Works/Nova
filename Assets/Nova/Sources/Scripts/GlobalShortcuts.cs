using System;
using UnityEngine;

namespace Nova
{
    public class GlobalShortcuts : MonoBehaviour
    {
        private InputMapper inputMapper;

        private void Awake()
        {
            inputMapper = Utils.FindNovaGameController().InputMapper;
        }

        private void Update()
        {
            if (inputMapper.GetKeyUp(AbstractKey.ToggleFullScreen))
            {
                GameRenderManager.SwitchFullScreen();
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
    }
}
