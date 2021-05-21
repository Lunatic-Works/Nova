using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class AvatarController : CompositeSpriteControllerBase
    {
        public string luaGlobalName;
        public float transitionDuration = 0.2f;
        public int visibleTextPushDistance = 100;

        private Dictionary<string, string> boundDialogueNameToImageName;
        private string pendingBindImageName; // null: no bind, "": bind to empty

        public int avatarWidth => string.IsNullOrEmpty(currentImageName) ? 0 : visibleTextPushDistance;

        protected override void Awake()
        {
            base.Awake();
            boundDialogueNameToImageName = new Dictionary<string, string>();
            pendingBindImageName = null;

            if (!string.IsNullOrEmpty(luaGlobalName))
            {
                LuaRuntime.Instance.BindObject(luaGlobalName, this, "_G");
                gameState.AddRestorable(this);
            }
        }

        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(luaGlobalName))
            {
                gameState.RemoveRestorable(this);
            }
        }

        public void SetDialogueName(string name)
        {
            if (pendingBindImageName != null)
            {
                boundDialogueNameToImageName[name] = pendingBindImageName;
                pendingBindImageName = null;
            }
            else
            {
                boundDialogueNameToImageName.TryGetValue(name, out string imageName);
                if (currentImageName == imageName)
                {
                    return;
                }

                if (!string.IsNullOrEmpty(imageName))
                {
                    string[] parts = StringToPoseArray(imageName);
                    if (parts.Length == 1)
                    {
                        base.SetImage(imageName);
                    }
                    else
                    {
                        base.SetPose(parts, true);
                    }
                }
                else
                {
                    base.ClearImage();
                }
            }
        }

        protected override void SetPose(string[] poseArray, bool fade)
        {
            base.SetPose(poseArray, fade);
            pendingBindImageName = PoseArrayToString(poseArray);
        }

        /// <summary>
        /// Bind current (i.e. next dialogue) dialogue name to have the target image as avatar
        /// </summary>
        public override void SetImage(string to, bool fade = true)
        {
            base.SetImage(to, fade);
            pendingBindImageName = to;
        }

        /// <summary>
        /// Clear avatar of current (i.e. next dialogue) dialogue name
        /// </summary>
        public override void ClearImage(bool fade = true)
        {
            base.ClearImage(fade);
            pendingBindImageName = "";
        }

        /// <summary>
        /// Clear all bindings, but does not hide current displayed avatar
        /// </summary>
        public void ResetAll()
        {
            boundDialogueNameToImageName.Clear();
        }

        private Color _color = Color.white;

        public override Color color
        {
            get => _color;
            set => SetColor(_color = value);
        }

        public override string restorableObjectName => luaGlobalName;

        [Serializable]
        private class AvatarRestoreData : CompositeSpriteControllerBaseRestoreData
        {
            public readonly Dictionary<string, string> boundDialogueNameToImageName;

            public AvatarRestoreData(CompositeSpriteControllerBaseRestoreData baseData,
                Dictionary<string, string> boundDialogueNameToImageName) : base(baseData)
            {
                this.boundDialogueNameToImageName = boundDialogueNameToImageName;
            }
        }

        public override IRestoreData GetRestoreData()
        {
            return new AvatarRestoreData(base.GetRestoreData() as CompositeSpriteControllerBaseRestoreData,
                boundDialogueNameToImageName);
        }

        public override void Restore(IRestoreData restoreData)
        {
            var data = restoreData as AvatarRestoreData;
            boundDialogueNameToImageName = data.boundDialogueNameToImageName;
            base.Restore(restoreData);
        }
    }
}