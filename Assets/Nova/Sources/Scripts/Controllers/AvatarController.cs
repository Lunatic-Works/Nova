using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [ExportCustomType]
    public class AvatarController : CompositeSpriteController
    {
        [SerializeField] private int textPadding;
        [SerializeField] private Camera renderCamera;
        [SerializeField] private RawImage image;

        private AvatarConfigs configs;
        private DialogueBoxController dialogueBox;
        private RectTransform rectTransform;

        [ReadOnly] public string characterName;
        private Dictionary<string, string> characterToPose = new Dictionary<string, string>();

        public int textPaddingOrZero => string.IsNullOrEmpty(currentPose) ? 0 : textPadding;
        public override string imageFolder => configs.GetImageFolder(characterName);
        public override bool renderToCamera => true;
        public override RenderTexture renderTexture => null;

        protected override void Awake()
        {
            base.Awake();

            configs = GetComponentInParent<AvatarConfigs>();
            dialogueBox = GetComponentInParent<DialogueBoxController>();
            rectTransform = GetComponent<RectTransform>();

            gameState.AddRestorable(this);

            gameState.nodeChanged.AddListener(OnNodeChanged);
        }

        private void Start()
        {
            var referenceWidth = (int)rectTransform.rect.width;
            var referenceHeight = (int)rectTransform.rect.height;
            var rt = new RenderTexture(referenceWidth, referenceHeight, 0, RenderTextureFormat.ARGB32)
            {
                name = "AvatarTexture"
            };
            renderCamera.targetTexture = rt;
            image.texture = rt;
            renderCamera.enabled = true;

            UpdateImage(false);
        }

        private void OnDestroy()
        {
            Destroy(renderCamera.targetTexture);

            gameState.RemoveRestorable(this);

            gameState.nodeChanged.RemoveListener(OnNodeChanged);
        }

        private bool CheckCharacterName(string pose)
        {
            if (string.IsNullOrEmpty(characterName))
            {
                Debug.LogWarning($"Nova: Set avatar {pose} with empty characterName.");
                return false;
            }

            if (!configs.Contains(characterName))
            {
                Debug.LogWarning($"Nova: Set avatar {pose} with unknown characterName {characterName}");
                return false;
            }

            return true;
        }

        public GameCharacterController GetCharacterController()
        {
            return configs.GetCharacterController(characterName);
        }

        public void SetPoseDelayed(string pose)
        {
            if (!CheckCharacterName(pose))
            {
                return;
            }

            characterToPose[characterName] = pose;
        }

        public void ClearImageDelayed(string characterName)
        {
            characterToPose.Remove(characterName);
        }

        public void ClearImageDelayed()
        {
            characterToPose.Remove(characterName);
        }

        public void UpdateImage(bool fade = true)
        {
            if (string.IsNullOrEmpty(characterName) || !configs.Contains(characterName) ||
                !characterToPose.ContainsKey(characterName))
            {
                ClearImage(fade);
            }
            else
            {
                SetPose(characterToPose[characterName], fade);
            }
        }

        private void SetAvatarRect(float pixelsPerUnit, Rect spriteBounds, Rect rect)
        {
            var x = spriteBounds.xMin + rect.center.x / pixelsPerUnit;
            var y = spriteBounds.yMax - rect.center.y / pixelsPerUnit;
            var size = rect.height / 2f / pixelsPerUnit * renderCamera.transform.lossyScale.y;
            var scale = renderCamera.orthographicSize / size;
            mergerPrimary.transform.localPosition = new Vector3(-x * scale, -y * scale, 0);
            mergerPrimary.transform.localScale = new Vector3(scale, scale, scale);
        }

        protected override void SetSprites(string pose, IReadOnlyList<SpriteWithOffset> sprites)
        {
            base.SetSprites(pose, sprites);

            if (!string.IsNullOrEmpty(pose) && sprites.Count > 0)
            {
                var pixelsPerUnit = sprites[0].sprite.pixelsPerUnit;
                var spriteBounds = CompositeSpriteMerger.GetMergedSize(sprites);
                SetAvatarRect(pixelsPerUnit, spriteBounds, configs.GetRect(characterName, pose));
            }
        }

        public override void SetPose(string pose, bool fade, float duration)
        {
            if (string.IsNullOrEmpty(currentPose) || string.IsNullOrEmpty(pose))
            {
                fade = false;
            }

            image.gameObject.SetActive(!string.IsNullOrEmpty(pose));

            base.SetPose(pose, fade, duration);
        }

        public void ResetAll()
        {
            characterToPose.Clear();
        }

        private void OnNodeChanged(NodeChangedData nodeChangedData)
        {
            ResetAll();
        }

        private void Update()
        {
            // TODO: optimize this by implementing SetPose callbacks
            renderCamera.enabled = needRender;
        }

        #region Restoration

        public override string restorableName => dialogueBox.restorableName + "_avatar";

        [Serializable]
        private class AvatarControllerRestoreData : CompositeSpriteControllerRestoreData
        {
            public readonly string characterName;
            public readonly Dictionary<string, string> characterToPose;

            public AvatarControllerRestoreData(AvatarController parent) : base(parent)
            {
                characterName = parent.characterName;
                characterToPose = parent.characterToPose.ToDictionary(x => x.Key, x => x.Value);
            }
        }

        public override IRestoreData GetRestoreData()
        {
            return new AvatarControllerRestoreData(this);
        }

        public override void Restore(IRestoreData restoreData)
        {
            var data = restoreData as AvatarControllerRestoreData;
            characterName = data.characterName;
            characterToPose = data.characterToPose;

            base.Restore(restoreData);
        }

        #endregion
    }
}
