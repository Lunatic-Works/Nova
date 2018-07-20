using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    public class BackgroundController : MonoBehaviour
    {
        public string imageFolder;
        private SpriteRenderer _spriteRenderer;

        private void Start()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            LuaRuntime.Instance.BindObject("backgroundController", this);
        }

        #region Methods called by external scripts

        /// <summary>
        /// Change the background image
        /// This method is designed to be called by external scripts
        /// </summary>
        /// <param name="imageName">The name of the image file</param>
        public void SetImage(string imageName)
        {
            _spriteRenderer.sprite = AssetsLoader.GetSprite(imageFolder + imageName);
        }

        #endregion
    }
}