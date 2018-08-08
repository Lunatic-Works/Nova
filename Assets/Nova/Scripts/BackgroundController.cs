using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;

namespace Nova
{
    public class BackgroundController : MonoBehaviour, IRestorable
    {
        public string imageFolder;

        private SpriteRenderer _spriteRenderer;
        private GameState gameState;

        private void Start()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            LuaRuntime.Instance.BindObject("backgroundController", this);
            gameState = GameState.Instance;
            gameState.AddRestorable(this);
        }

        private string currentImageName;

        #region Methods called by external scripts

        /// <summary>
        /// Change the background image
        /// This method is designed to be called by external scripts
        /// </summary>
        /// <param name="imageName">The name of the image file</param>
        public void SetImage(string imageName)
        {
            _spriteRenderer.sprite = AssetsLoader.GetSprite(System.IO.Path.Combine(imageFolder, imageName));
            currentImageName = imageName;
        }

        public void ClearImage()
        {
            _spriteRenderer.sprite = null;
            currentImageName = null;
        }

        #endregion

        [Serializable]
        private class RestoreData : IRestoreData
        {
            public string currentImageName { get; private set; }

            public RestoreData(string currentImageName)
            {
                this.currentImageName = currentImageName;
            }
        }

        public string restorableObjectName
        {
            get { return "backgroundController"; }
        }

        public IRestoreData GetRestoreData()
        {
            return new RestoreData(currentImageName);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as RestoreData;
            if (data.currentImageName != null)
            {
                SetImage(data.currentImageName);
            }
        }
    }
}