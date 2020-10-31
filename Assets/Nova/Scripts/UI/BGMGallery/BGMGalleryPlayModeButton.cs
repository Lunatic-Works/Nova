using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class BGMGalleryPlayModeButton : MonoBehaviour
    {
        public Image iconImage;
        public List<Sprite> icons;
        public string configKey = "nova.bgm_gallery.play_mode";

        private BGMGalleryController controller;
        private ConfigManager config;

        private MusicListMode GetNextMode()
        {
            return (MusicListMode) (((int) controller.mode + 1) % 3);
        }

        private string GetDescription()
        {
            switch (controller.mode)
            {
                case MusicListMode.Sequential:
                    return I18n.__("bgmgallery.mode.seq");
                case MusicListMode.SingleLoop:
                    return I18n.__("bgmgallery.mode.loop");
                case MusicListMode.Random:
                    return I18n.__("bgmgallery.mode.rand");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Awake()
        {
            controller = GetComponentInParent<BGMGalleryController>();
            config = Utils.FindNovaGameController().ConfigManager;
        }

        private MusicListMode mode
        {
            set
            {
                controller.mode = value;
                iconImage.sprite = icons[(int) value];
            }
        }

        private void LoadConfig()
        {
            mode = (MusicListMode) config.GetInt(configKey, (int) controller.mode);
        }

        private void OnEnable()
        {
            LoadConfig();
        }

        private void OnDisable()
        {
            config.SetInt(configKey, (int) controller.mode);
        }

        public void OnClick()
        {
            mode = GetNextMode();
        }
    }
}