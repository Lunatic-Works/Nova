using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [ExportCustomType]
    public class DialogueBoxColor
    {
        [ExportCustomType]
        public enum Type
        {
            Text,
            Background
        }

        private readonly DialogueBoxController dialogueBoxController;
        private readonly Type type;

        public DialogueBoxColor(DialogueBoxController dialogueBoxController, Type type)
        {
            this.dialogueBoxController = dialogueBoxController;
            this.type = type;
        }

        public Color color
        {
            get
            {
                switch (type)
                {
                    case Type.Text:
                        return dialogueBoxController.textColor;
                    case Type.Background:
                        return dialogueBoxController.backgroundColor;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                switch (type)
                {
                    case Type.Text:
                        dialogueBoxController.textColor = value;
                        break;
                    case Type.Background:
                        dialogueBoxController.backgroundColor = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    [ExportCustomType]
    public class DialogueBoxController : PanelController, IRestorable
    {
        public enum DialogueUpdateMode
        {
            Overwrite,
            Append
        }

        [SerializeField] private DialogueUpdateMode dialogueUpdateMode;

        private GameState gameState;
        private DialogueState dialogueState;
        private GameViewController gameView;

        private ScrollRect dialogueTextScrollRect;
        private DialogueTextController dialogueText;
        private RectTransform dialogueTextRect;

        public AvatarController avatar { get; private set; }

        public RectTransform rect { get; private set; }

        [SerializeField] private GameObject backgroundPrefab;
        private Image backgroundImage;
        private CanvasGroup backgroundCanvasGroup;
        private Button hideDialogueButton;
        private GameObject dialogueFinishIcon;

        private Color _backgroundColor;
        private float _configOpacity;

        private void UpdateColor()
        {
            backgroundImage.color = new Color(_backgroundColor.r, _backgroundColor.g, _backgroundColor.b, 1f);
            backgroundCanvasGroup.alpha = _backgroundColor.a * configOpacity;
        }

        public Color backgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                if (inited)
                {
                    UpdateColor();
                }
            }
        }

        public float configOpacity
        {
            get => _configOpacity;
            set
            {
                _configOpacity = value;
                if (inited)
                {
                    UpdateColor();
                }
            }
        }

        public bool isCurrent => gameView.currentDialogueBox == this;

        public string luaGlobalName;

        protected override bool Init()
        {
            if (base.Init())
            {
                return true;
            }

            var controller = Utils.FindNovaController();
            gameState = controller.GameState;
            dialogueState = controller.DialogueState;
            gameView = GetComponentInParent<GameViewController>();

            dialogueTextScrollRect = GetComponentInChildren<ScrollRect>();
            dialogueText = GetComponentInChildren<DialogueTextController>();
            dialogueTextRect = dialogueText.transform as RectTransform;

            avatar = GetComponentInChildren<AvatarController>();

            rect = transform.Find("DialoguePanel").GetComponent<RectTransform>();

            var background = Instantiate(backgroundPrefab, myPanel.transform, false) as GameObject;
            background.transform.SetSiblingIndex(0);
            backgroundImage = background.GetComponent<Image>();
            backgroundCanvasGroup = background.GetComponent<CanvasGroup>();
            hideDialogueButton = background.transform.Find("CloseButton").GetComponent<Button>();
            dialogueFinishIcon = background.transform.Find("DialogueFinishIcon").gameObject;

            UpdateColor();
            hideDialogueButton.onClick.AddListener(OnCloseButtonClick);

            if (!string.IsNullOrEmpty(luaGlobalName))
            {
                LuaRuntime.Instance.BindObject(luaGlobalName, this, "_G");
                gameState.AddRestorable(this);
            }

            this.HideImmediate();

            return false;
        }

        private void OnDestroy()
        {
            gameState.RemoveRestorable(this);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            dialogueState.state = DialogueState.State.Normal;
        }

        public void ShowDialogueFinishIcon(bool to)
        {
            dialogueFinishIcon.SetActive(to);
        }

        public void OnDialogueWillChange()
        {
            ResetTextAnimationConfig();
            ShowDialogueFinishIcon(false);
        }

        public void DisplayDialogue(DialogueDisplayData displayData)
        {
            switch (dialogueUpdateMode)
            {
                case DialogueUpdateMode.Overwrite:
                    OverwriteDialogue(displayData);
                    break;
                case DialogueUpdateMode.Append:
                    AppendDialogue(displayData);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void NewPage()
        {
            dialogueText.Clear();
        }

        public void OnCloseButtonClick()
        {
            gameView.HideUI();
        }

        [SerializeField] private NovaAnimation textAnimation;

        private float textAnimationDelay;
        private float textDurationOverride = -1f;
        private bool textScrollOverriden;

        private void ResetTextAnimationConfig()
        {
            textAnimationDelay = 0f;
            textDurationOverride = -1f;
            textScrollOverriden = false;
        }

        public void SetTextAnimationDelay(float secs)
        {
            textAnimationDelay = Mathf.Max(secs, 0f);
        }

        public void OverrideTextDuration(float secs)
        {
            textDurationOverride = Mathf.Max(secs, 0f);
        }

        public void OverrideTextScroll()
        {
            textScrollOverriden = true;
        }

        // dialogueTextScrollRect.verticalNormalizedPosition cannot be out of [0, 1],
        // and it may be out of sync with dialogueTextRect.localPosition.y when the content changes
        public void SetTextScroll(float value)
        {
            var position = dialogueTextRect.localPosition;
            dialogueTextRect.localPosition = new Vector3(position.x, value, position.z);
        }

        public RectTransformAnimationProperty GetTextScrollAnimationProperty(float start, float target)
        {
            var x = dialogueTextRect.localPosition.x;
            return new RectTransformAnimationProperty(dialogueTextRect, new Vector2(x, start), new Vector2(x, target),
                true);
        }

        public float characterFadeInDuration { get; set; }

        private void AppendDialogue(DialogueDisplayData displayData, bool needAnimation = true)
        {
            Color nowTextColor =
                textColorHasSet ? textColor : dialogueState.isDialogueReached ? readColor : unreadColor;
            textLeftExtraPadding = avatar?.textPaddingOrZero ?? 0;
            var entry = dialogueText.AddEntry(displayData, textAlignment, nowTextColor, nowTextColor, materialName,
                dialogueEntryLayoutSetting, textLeftExtraPadding);

            if (needAnimation && !gameState.isRestoring && !dialogueState.isFastForward)
            {
                var contentProxy = entry.contentProxy;

                float textDuration;
                if (textDurationOverride >= 0f)
                {
                    textDuration = textDurationOverride;
                }
                else
                {
                    textDuration = characterFadeInDuration * contentProxy.GetPageCharacterCount();
                }

                var animEntry = textAnimation
                    .Do(new ActionAnimationProperty(() => contentProxy.SetTextAlpha(0))) // hide text
                    .Then(null, textAnimationDelay)
                    .Then(
                        new TextFadeInAnimationProperty(contentProxy, (byte)(255 * nowTextColor.a)),
                        textDuration
                    );
                if (!textScrollOverriden)
                {
                    if (dialogueText.Count == 1)
                    {
                        SetTextScroll(0f);
                    }
                    else
                    {
                        animEntry.And(
                            new VerticalScrollRectAnimationProperty(dialogueTextScrollRect, 0f),
                            textDuration,
                            AnimationEntry.CubicEasing(0f, 1f)
                        );
                    }
                }
            }
            else
            {
                if (!textScrollOverriden)
                {
                    SetTextScroll(0f);
                }
            }
        }

        private void OverwriteDialogue(DialogueDisplayData displayData)
        {
            NewPage();
            AppendDialogue(displayData);
        }

        public bool NextPageOrStep()
        {
            if (dialogueText.Count == 0 || !dialogueText.dialogueEntryControllers.Last().Forward())
            {
                gameState.Step();
                return false;
            }

            return true;
        }

        public void ForceStep()
        {
            NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
            gameState.Step();
        }

        #region Properties for dialogue entries

        private TextAlignmentOptions _textAlignment;

        public TextAlignmentOptions textAlignment
        {
            get => _textAlignment;
            set
            {
                _textAlignment = value;
                foreach (var dec in dialogueText.dialogueEntryControllers)
                {
                    dec.alignment = value;
                }
            }
        }

        [SerializeField] private Color readColor;
        [SerializeField] private Color unreadColor;

        [HideInInspector] public bool textColorHasSet;

        private Color _textColor;

        public Color textColor
        {
            get => _textColor;
            set
            {
                _textColor = value;
                foreach (var dec in dialogueText.dialogueEntryControllers)
                {
                    dec.textColor = value;
                }
            }
        }

        private string _materialName;

        public string materialName
        {
            get => _materialName;
            set
            {
                _materialName = value;
                foreach (var dec in dialogueText.dialogueEntryControllers)
                {
                    dec.materialName = value;
                }
            }
        }

        [SerializeField] private DialogueEntryLayoutSetting dialogueEntryLayoutSetting;

        private int _textLeftExtraPadding;

        // Modified only by theme
        private int textLeftExtraPadding
        {
            get => _textLeftExtraPadding;
            set
            {
                _textLeftExtraPadding = value;
                foreach (var dec in dialogueText.dialogueEntryControllers)
                {
                    dec.textLeftExtraPadding = value;
                }
            }
        }

        // Avoid refreshing text proxy when changing size in animation
        private bool _canRefreshTextProxy = true;

        public bool canRefreshTextProxy
        {
            get => _canRefreshTextProxy;
            set
            {
                _canRefreshTextProxy = value;
                foreach (var dec in dialogueText.dialogueEntryControllers)
                {
                    dec.canRefreshTextProxy = value;
                }
            }
        }

        public string FindIntersectingLink(Vector3 position, Camera camera)
        {
            return dialogueText.dialogueEntryControllers.Select(dec => dec.FindIntersectingLink(position, camera))
                .FirstOrDefault(link => !string.IsNullOrEmpty(link));
        }

        #endregion

        #region Show/Hide close button and dialogue finish icon

        private bool _closeButtonShown = true;

        public bool closeButtonShown
        {
            get => _closeButtonShown;
            set
            {
                if (_closeButtonShown == value)
                {
                    return;
                }

                _closeButtonShown = value;
                hideDialogueButton.gameObject.SetActive(value);
            }
        }

        public bool dialogueFinishIconShown { get; private set; } = true;

        #endregion

        #region Restoration

        public string restorableName => luaGlobalName;

        [Serializable]
        private class DialogueBoxControllerRestoreData : IRestoreData
        {
            public readonly bool active;
            public readonly RectTransformData rectTransformData;
            public readonly Vector4Data backgroundColor;
            public readonly DialogueUpdateMode dialogueUpdateMode;
            public readonly List<DialogueDisplayData> displayDatas;
            public readonly int textAlignment;
            public readonly bool textColorHasSet;
            public readonly Vector4Data textColor;
            public readonly string materialName;
            public readonly bool closeButtonShown;
            public readonly bool dialogueFinishIconShown;

            public DialogueBoxControllerRestoreData(DialogueBoxController controller)
            {
                active = controller.active;
                rectTransformData = new RectTransformData(controller.rect);
                backgroundColor = controller.backgroundColor;
                dialogueUpdateMode = controller.dialogueUpdateMode;
                displayDatas = controller.dialogueText.dialogueEntryControllers.Select(x => x.displayData).ToList();
                textAlignment = (int)controller.textAlignment;
                textColorHasSet = controller.textColorHasSet;
                textColor = controller.textColor;
                materialName = controller.materialName;
                closeButtonShown = controller.closeButtonShown;
                dialogueFinishIconShown = controller.dialogueFinishIconShown;
            }
        }

        public IRestoreData GetRestoreData()
        {
            return new DialogueBoxControllerRestoreData(this);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as DialogueBoxControllerRestoreData;

            myPanel.SetActive(data.active);
            data.rectTransformData.Restore(rect);
            backgroundColor = data.backgroundColor;

            dialogueUpdateMode = data.dialogueUpdateMode;

            textAlignment = (TextAlignmentOptions)data.textAlignment;
            textColorHasSet = data.textColorHasSet;
            textColor = data.textColor;
            materialName = data.materialName;

            NewPage();
            // TODO: Restore displayDatas from FlowChartGraph, like in LogController
            foreach (var displayData in data.displayDatas)
            {
                AppendDialogue(displayData, needAnimation: false);
            }

            closeButtonShown = data.closeButtonShown;
            dialogueFinishIconShown = data.dialogueFinishIconShown;
        }

        #endregion
    }
}
