using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.EventSystems;
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

        private readonly DialogueBoxController dialogueBox;
        private readonly Type type;

        public DialogueBoxColor(DialogueBoxController dialogueBox, Type type)
        {
            this.dialogueBox = dialogueBox;
            this.type = type;
        }

        public Color color
        {
            get
            {
                switch (type)
                {
                    case Type.Text:
                        return dialogueBox.textColor;
                    case Type.Background:
                        return dialogueBox.backgroundColor;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                switch (type)
                {
                    case Type.Text:
                        dialogueBox.textColor = value;
                        break;
                    case Type.Background:
                        dialogueBox.backgroundColor = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    [ExportCustomType]
    public partial class DialogueBoxController : ViewControllerBase, IPointerDownHandler, IPointerUpHandler,
        IDialogueBoxController, IRestorable
    {
        [ExportCustomType]
        public enum DialogueUpdateMode
        {
            Overwrite,
            Append
        }

        public DialogueUpdateMode dialogueUpdateMode;

        private GameState gameState;
        private GameController gameController;
        private ConfigManager configManager;

        private TextSpeedConfigReader textSpeedConfigReader;
        private ScrollRect dialogueTextScrollRect;
        private VerticalLayoutGroup dialogueTextVerticalLayoutGroup;
        private DialogueTextController dialogueText;
        public List<Button> hideDialogueButtons;

        private ButtonRingTrigger buttonRingTrigger;
        private LogController logController;
        public AvatarController avatarController;

        public int themedEntryLeftPadding;
        public int themedEntryRightPadding;
        public float themedEntryNameTextSpacing;
        public float themedEntryPreferredHeight;

        // TODO: there are a lot of magic numbers for the current UI
        public bool useThemedBox
        {
            get => backgroundGO.activeSelf;
            set
            {
                dialogueText.layoutSetting = new DialogueEntryLayoutSetting
                {
                    leftPadding = value ? themedEntryLeftPadding : 0,
                    rightPadding = value ? themedEntryRightPadding : 0,
                    nameTextSpacing = value ? themedEntryNameTextSpacing : 0,
                    preferredHeight = value ? (float?)themedEntryPreferredHeight : null
                };

                var scrollRectTransform = dialogueTextScrollRect.transform as RectTransform;

                backgroundGO.SetActive(value);
                basicBackgroundGO.SetActive(!value);
                if (value)
                {
                    scrollRectTransform.offsetMin = new Vector2(0, 0);
                    scrollRectTransform.offsetMax = new Vector2(0, 0);
                    dialogueTextVerticalLayoutGroup.padding = new RectOffset(60, 120, 42, 0);
                }
                else
                {
                    if (scrollRectTransform.rect.height > 360f)
                    {
                        scrollRectTransform.offsetMin = new Vector2(60, 60);
                        scrollRectTransform.offsetMax = new Vector2(-120, -42);
                        dialogueTextVerticalLayoutGroup.padding = new RectOffset(0, 0, 0, 120);
                    }
                    else
                    {
                        scrollRectTransform.offsetMin = new Vector2(60, 0);
                        scrollRectTransform.offsetMax = new Vector2(-120, -42);
                        dialogueTextVerticalLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
                    }
                }
            }
        }

        public RectTransform rect { get; private set; }

        public GameObject basicBackgroundGO;
        public GameObject backgroundGO;

        public Image basicBackgroundImage { get; private set; }
        public CanvasGroup backgroundCanvasGroup { get; private set; }

        private Color _backgroundColor;

        public Color backgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                Init();
                basicBackgroundImage.color = new Color(_backgroundColor.r, _backgroundColor.g, _backgroundColor.b,
                    _backgroundColor.a * _configOpacity);
                backgroundCanvasGroup.alpha = _backgroundColor.a * _configOpacity;
            }
        }

        private float _configOpacity;

        public float configOpacity
        {
            get => _configOpacity;
            set
            {
                _configOpacity = value;
                backgroundColor = backgroundColor;
            }
        }

        private bool inited;

        private void Init()
        {
            if (inited)
            {
                return;
            }

            gameController = Utils.FindNovaGameController();
            gameState = gameController.GameState;
            configManager = gameController.ConfigManager;

            textSpeedConfigReader = GetComponent<TextSpeedConfigReader>();
            dialogueTextScrollRect = GetComponentInChildren<ScrollRect>(true);
            dialogueText = GetComponentInChildren<DialogueTextController>(true);
            dialogueTextVerticalLayoutGroup = dialogueText.GetComponent<VerticalLayoutGroup>();

            buttonRingTrigger = GetComponentInChildren<ButtonRingTrigger>();
            logController = transform.parent.GetComponentInChildren<LogController>();

            Transform dialoguePanel = transform.Find("DialoguePanel");
            rect = dialoguePanel.GetComponent<RectTransform>();
            basicBackgroundImage = basicBackgroundGO.GetComponent<Image>();
            backgroundCanvasGroup = backgroundGO.GetComponent<CanvasGroup>();

            uiPP = UICameraHelper.Active.GetComponent<PostProcessing>();

            foreach (var btn in hideDialogueButtons)
                btn.onClick.AddListener(Hide);
            dialogueFinished = new AndGate(to =>
            {
                foreach (var icon in dialogueFinishIcons)
                    icon.SetActive(to);
            });
            dialogueBoxShown = new AndGate(dialogueFinished);
            dialogueBoxShown.SetActive(true);

            LuaRuntime.Instance.BindObject("dialogueBoxController", this);

            inited = true;
        }

        protected override void Awake()
        {
            base.Awake();
            Init();
        }

        public override void Hide(Action onFinish)
        {
            state = DialogueBoxState.Normal;
            base.Hide(onFinish);
            dialogueBoxShown.SetActive(false);
        }

        public override void Show(Action onFinish)
        {
            base.Show(onFinish);
            dialogueBoxShown.SetActive(true);
        }

        private void OnEnable()
        {
            gameState.DialogueWillChange += OnDialogueWillChange;
            gameState.DialogueChanged += OnDialogueChanged;
            gameState.BranchSelected += OnBranchSelected;
            gameState.CurrentRouteEnded += OnCurrentRouteEnded;
            gameState.AddRestorable(this);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            gameState.DialogueWillChange -= OnDialogueWillChange;
            gameState.DialogueChanged -= OnDialogueChanged;
            gameState.BranchSelected -= OnBranchSelected;
            gameState.CurrentRouteEnded -= OnCurrentRouteEnded;
            gameState.RemoveRestorable(this);
        }

        private void OnCurrentRouteEnded(CurrentRouteEndedData arg0)
        {
            state = DialogueBoxState.Normal;
            this.SwitchView<TitleController>();
        }

        private void OnDialogueWillChange()
        {
            StopTimer();
            ResetTextAnimationConfig();
            dialogueFinished.SetActive(false);
        }

        protected override void Update()
        {
            if (viewManager.currentView == CurrentViewType.Game && dialogueAvailable)
            {
                timeAfterDialogueChange += Time.deltaTime;

                if (dialogueFinishIconEnabled &&
                    state == DialogueBoxState.Normal && viewManager.currentView != CurrentViewType.InTransition &&
                    timeAfterDialogueChange > dialogueTime)
                {
                    dialogueFinished.SetActive(true);
                }
            }

            // All shortcut should use InputMapping. Always call this method in update as validity of each AbstractKeys
            // are defined specifically.
            HandleShortcut();

            if (!gameController.inputDisabled)
            {
                HandleInput();
            }

            // Refresh text when size changes
            if (rect.hasChanged)
            {
                // Debug.Log("Dialogue box size changed");

                foreach (var dec in dialogueText.dialogueEntryControllers)
                {
                    dec.ScheduleRefresh();
                }

                rect.hasChanged = false;
            }
        }

        public Material fastForwardPostProcessingMaterial;
        public GameObject fastForwardModeIcon;
        public GameObject autoModeIcon;
        public List<GameObject> dialogueFinishIcons;

        private AndGate dialogueBoxShown;
        private AndGate dialogueFinished;
        private PostProcessing uiPP;

        private bool isReadDialogue = false;

        private DialogueBoxState _state = DialogueBoxState.Normal;

        /// <summary>
        /// Current state of the dialogue box
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public DialogueBoxState state
        {
            get => _state;
            set
            {
                if (_state == value)
                {
                    return;
                }

                switch (_state)
                {
                    case DialogueBoxState.Normal:
                        break;
                    case DialogueBoxState.Auto:
                        StopAuto();
                        autoModeStops.Invoke();

                        if (autoModeIcon != null)
                        {
                            autoModeIcon.SetActive(false);
                        }

                        break;
                    case DialogueBoxState.FastForward:
                        StopFastForward();
                        fastForwardModeStops.Invoke();

                        uiPP.ClearLayer();
                        if (fastForwardModeIcon != null)
                        {
                            fastForwardModeIcon.SetActive(false);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                switch (value)
                {
                    case DialogueBoxState.Normal:
                        _state = DialogueBoxState.Normal;
                        break;
                    case DialogueBoxState.Auto:
                        StartAuto();
                        autoModeStarts.Invoke();

                        if (autoModeIcon != null)
                        {
                            autoModeIcon.SetActive(true);
                        }

                        break;
                    case DialogueBoxState.FastForward:
                        if (!isReadDialogue && onlyFastForwardRead && !fastForwardHotKeyHolding)
                        {
                            Alert.Show(I18n.__("dialogue.noreadtext"));
                            return;
                        }

                        StartFastForward();
                        fastForwardModeStarts.Invoke();

                        uiPP.PushMaterial(fastForwardPostProcessingMaterial);
                        if (fastForwardModeIcon != null)
                        {
                            fastForwardModeIcon.SetActive(true);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public UnityEvent autoModeStarts;
        public UnityEvent autoModeStops;
        public UnityEvent fastForwardModeStarts;
        public UnityEvent fastForwardModeStops;

        private string currentNodeName;

        /// <summary>
        /// The content of the dialogue box needs to be changed
        /// </summary>
        /// <param name="dialogueData"></param>
        private void OnDialogueChanged(DialogueChangedData dialogueData)
        {
            RestartTimer();

            currentNodeName = dialogueData.nodeName;

            isReadDialogue = dialogueData.hasBeenReached;
            if (state == DialogueBoxState.FastForward && !isReadDialogue && onlyFastForwardRead &&
                !fastForwardHotKeyHolding)
            {
                state = DialogueBoxState.Normal;
            }

            switch (dialogueUpdateMode)
            {
                case DialogueUpdateMode.Overwrite:
                    OverwriteDialogue(dialogueData.displayData);
                    break;
                case DialogueUpdateMode.Append:
                    AppendDialogue(dialogueData.displayData);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // No animation playing when fast forwarding
            if (state == DialogueBoxState.FastForward)
            {
                NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
            }

            // Check current state and set schedule for the next dialogue entry
            SetSchedule();

            dialogueTime = GetDialogueTime();
        }

        private void JumpChapter(int offset)
        {
            var chapters = gameState.GetAllStartNodeNames();
            int targetChapterIndex = chapters.IndexOf(currentNodeName) + offset;
            if (targetChapterIndex >= 0 && targetChapterIndex < chapters.Count)
            {
                NovaAnimation.StopAll();
                gameState.ResetGameState();
                gameState.GameStart(chapters[targetChapterIndex]);
            }
            else
            {
                Debug.LogWarning($"Nova: No chapter index {targetChapterIndex}");
            }
        }

        private void SetSchedule()
        {
            TryRemoveSchedule();
            switch (state)
            {
                case DialogueBoxState.Normal:
                    break;
                case DialogueBoxState.Auto:
                    TrySchedule(GetDialogueTimeAuto());
                    break;
                case DialogueBoxState.FastForward:
                    TrySchedule(fastForwardDelay);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void NewPage()
        {
            dialogueText.Clear();
        }

        private bool useDefaultTextAnimation = true;
        private float textAnimationDelay = 0;

        private void ResetTextAnimationConfig()
        {
            useDefaultTextAnimation = true;
            textAnimationDelay = 0;
        }

        public void PreventDefaultTextAnimation()
        {
            useDefaultTextAnimation = false;
        }

        public void SetTextAnimationDelay(float secs)
        {
            textAnimationDelay = Mathf.Max(secs, 0);
        }

        private float perCharacterFadeInDuration => textSpeedConfigReader.perCharacterFadeInDuration;

        private void AppendDialogue(DialogueDisplayData displayData, bool needAnimation = true)
        {
            Color nowTextColor = textColorHasSet ? textColor : isReadDialogue ? readColor : unreadColor;
            dialogueText.textLeftExtraPadding = avatarController.textPaddingOrZero;
            var entry = dialogueText.AddEntry(displayData, textAlignment, nowTextColor, nowTextColor, materialName);

            if (this.needAnimation && useDefaultTextAnimation && needAnimation && !gameState.isMovingBack &&
                state != DialogueBoxState.FastForward)
            {
                var contentProxy = entry.contentProxy;
                var characterCount = contentProxy.textBox.GetTextInfo(contentProxy.text).characterCount;

                // TODO: sometimes textInfo.characterCount returns 0, use text.Length
                if (characterCount <= 0 && contentProxy.text.Length > 0)
                {
                    Debug.LogWarning(
                        $"characterCount mismatch: {characterCount} {contentProxy.text.Length} {contentProxy.text}");
                    characterCount = contentProxy.text.Length;
                }

                float textAnimDuration = perCharacterFadeInDuration * characterCount;

                // TMP has many strange behaviour, if the character animation looks strange, uncomment following
                // lines to debug
                // Debug.LogFormat("pc duration: {0}, char cnt: {1}, total duration: {2}, text: {3}",
                //      PerCharacterFadeInDuration,
                //      contentBox.textInfo.characterCount, textAnimDuration, contentBox.text);

                textAnimation.Do(new ActionAnimationProperty(() => contentProxy.SetTextAlpha(0))) // hide text
                    .Then(null).For(textAnimationDelay)
                    .Then(
                        new TextFadeInAnimationProperty(contentProxy, (byte)(255 * nowTextColor.a)),
                        textAnimDuration
                    ).And(
                        new VerticalScrollRectAnimationProperty(dialogueTextScrollRect, 0f),
                        textAnimDuration,
                        AnimationEntry.CubicEasing(0f, 1f)
                    );
            }
            else
            {
                dialogueTextScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void OverwriteDialogue(DialogueDisplayData displayData)
        {
            NewPage();
            AppendDialogue(displayData);
        }

        public bool continueAutoAfterBranch;
        public bool continueFastForwardAfterBranch;

        /// <summary>
        /// Check if state should be reset to Normal after the branch
        /// </summary>
        /// <param name="branchSelectedData"></param>
        private void OnBranchSelected(BranchSelectedData branchSelectedData)
        {
            if (branchSelectedData.selectedBranchInformation.mode == BranchMode.Jump)
            {
                return;
            }

            if (!continueAutoAfterBranch && state == DialogueBoxState.Auto)
            {
                state = DialogueBoxState.Normal;
            }

            if (!continueFastForwardAfterBranch && state == DialogueBoxState.FastForward)
            {
                state = DialogueBoxState.Normal;
            }
        }

        private Coroutine scheduledStepCoroutine = null;

        private void TrySchedule(float scheduledDelay)
        {
            if (dialogueAvailable)
            {
                scheduledStepCoroutine = StartCoroutine(ScheduledStep(scheduledDelay));
            }
        }

        private void TryRemoveSchedule()
        {
            if (scheduledStepCoroutine == null) return;
            StopCoroutine(scheduledStepCoroutine);
            scheduledStepCoroutine = null;
        }

        private static float GetDialogueTime(float offset = 0.0f, float voiceOffset = 0.0f)
        {
            return Mathf.Max(
                NovaAnimation.GetTotalTimeRemaining(AnimationType.PerDialogue | AnimationType.Text) + offset,
                CharacterController.MaxVoiceDurationOfNextDialogue + voiceOffset
            );
        }

        private float GetDialogueTimeAuto()
        {
            return Mathf.Max(
                GetDialogueTime(autoDelay, autoDelay * 0.5f),
                NovaAnimation.GetTotalTimeRemaining(AnimationType.Text) / perCharacterFadeInDuration * autoDelay * 0.1f
            );
        }

        /// <summary>
        /// Start auto
        /// </summary>
        /// <remarks>
        /// This method should be called when the state is normal
        /// </remarks>
        private void StartAuto()
        {
            Assert.AreEqual(state, DialogueBoxState.Normal, "DialogueBoxState State != DialogueBoxState.Normal");
            _state = DialogueBoxState.Auto;
            TrySchedule(GetDialogueTimeAuto());
        }

        /// <summary>
        /// Stop Auto
        /// </summary>
        /// <remarks>
        /// This method should be called when the state is auto
        /// </remarks>
        private void StopAuto()
        {
            Assert.AreEqual(state, DialogueBoxState.Auto, "DialogueBoxState State != DialogueBoxState.Auto");
            _state = DialogueBoxState.Normal;
            TryRemoveSchedule();
        }

        public float autoDelay;
        public float fastForwardDelay;

        [SerializeField] private NovaAnimation textAnimation;
        public bool needAnimation = true;

        /// <summary>
        /// Begin fast forward
        /// </summary>
        /// <remarks>
        /// This method should be called when the state is normal
        /// </remarks>
        private void StartFastForward()
        {
            Assert.AreEqual(state, DialogueBoxState.Normal, "DialogueBoxState State != DialogueBoxState.Normal");
            _state = DialogueBoxState.FastForward;
            TrySchedule(fastForwardDelay);
        }

        /// <summary>
        /// Stop fast forward
        /// </summary>
        /// <remarks>
        /// This method should be called when the state is fast forward
        /// </remarks>
        private void StopFastForward()
        {
            Assert.AreEqual(state, DialogueBoxState.FastForward,
                "DialogueBoxState State != DialogueBoxState.FastForward");
            _state = DialogueBoxState.Normal;
            TryRemoveSchedule();
        }

        private bool NextPageOrStep()
        {
            if (!useThemedBox || dialogueText.dialogueEntryControllers.Count != 1 ||
                !dialogueText.dialogueEntryControllers[0].Forward())
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

        private IEnumerator ScheduledStep(float scheduledDelay)
        {
            this.RuntimeAssert(dialogueAvailable, "Dialogue should be available when scheduling a step for it.");
            while (scheduledDelay > timeAfterDialogueChange)
            {
                yield return new WaitForSeconds(scheduledDelay - timeAfterDialogueChange);
            }

            // Pause one frame before step
            // Give time for rendering and can stop schedule step in time before any unwanted effects occurs
            yield return null;

            if (gameState.canStepForward)
            {
                NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                if (NextPageOrStep())
                {
                    timeAfterDialogueChange = 0;
                    TrySchedule(state == DialogueBoxState.Auto ? autoDelay : fastForwardDelay);
                }
            }
            else
            {
                state = DialogueBoxState.Normal;
            }
        }

        private float timeAfterDialogueChange;

        private float dialogueTime = float.MaxValue;

        private bool dialogueAvailable;

        private void StopTimer()
        {
            timeAfterDialogueChange = 0;
            dialogueAvailable = false;
        }

        private void RestartTimer()
        {
            timeAfterDialogueChange = 0;
            dialogueAvailable = true;
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

        public Color readColor;
        public Color unreadColor;

        [HideInInspector] public bool textColorHasSet = false;

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

        #endregion

        #region Show/Hide close button and dialogue finish icon

        private bool closeButtonShown = true;

        public void ShowCloseButton()
        {
            closeButtonShown = true;
            foreach (var btn in hideDialogueButtons)
            {
                btn.gameObject.SetActive(true);
            }
        }

        public void HideCloseButton()
        {
            closeButtonShown = false;
            foreach (var btn in hideDialogueButtons)
            {
                btn.gameObject.SetActive(false);
            }
        }

        public bool dialogueFinishIconEnabled = true;

        #endregion

        #region Restoration

        public string restorableObjectName => "dialogueBoxController";

        [Serializable]
        private class DialogueBoxRestoreData : IRestoreData
        {
            public readonly RectTransformRestoreData rectTransformRestoreData;
            public readonly Vector4Data backgroundColor;
            public readonly DialogueUpdateMode dialogueUpdateMode;
            public readonly List<DialogueDisplayData> displayDatas;
            public readonly bool canClickForward;
            public readonly bool scriptCanAbortAnimation;
            public readonly int textAlignment;
            public readonly bool useThemedBox;
            public readonly bool textColorHasSet;
            public readonly Vector4Data textColor;
            public readonly string materialName;
            public readonly bool closeButtonShown;
            public readonly bool dialogueFinishIconEnabled;

            public DialogueBoxRestoreData(RectTransform rect, Color backgroundColor,
                DialogueUpdateMode dialogueUpdateMode, List<DialogueDisplayData> displayDatas, bool canClickForward,
                bool scriptCanAbortAnimation, int textAlignment, bool useThemedBox, bool textColorHasSet,
                Color textColor, string materialName, bool closeButtonShown, bool dialogueFinishIconEnabled)
            {
                rectTransformRestoreData = new RectTransformRestoreData(rect);
                this.backgroundColor = backgroundColor;
                this.dialogueUpdateMode = dialogueUpdateMode;
                this.displayDatas = displayDatas;
                this.canClickForward = canClickForward;
                this.scriptCanAbortAnimation = scriptCanAbortAnimation;
                this.textAlignment = textAlignment;
                this.useThemedBox = useThemedBox;
                this.textColorHasSet = textColorHasSet;
                this.textColor = textColor;
                this.materialName = materialName;
                this.closeButtonShown = closeButtonShown;
                this.dialogueFinishIconEnabled = dialogueFinishIconEnabled;
            }
        }

        public IRestoreData GetRestoreData()
        {
            var displayDatas = dialogueText.dialogueEntryControllers.Select(x => x.displayData).ToList();
            return new DialogueBoxRestoreData(rect, backgroundColor, dialogueUpdateMode, displayDatas,
                canClickForward, scriptCanAbortAnimation, (int)textAlignment, useThemedBox, textColorHasSet,
                textColor, materialName, closeButtonShown, dialogueFinishIconEnabled);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as DialogueBoxRestoreData;
            data.rectTransformRestoreData.Restore(rect);
            backgroundColor = data.backgroundColor;

            dialogueUpdateMode = data.dialogueUpdateMode;
            canClickForward = data.canClickForward;
            scriptCanAbortAnimation = data.scriptCanAbortAnimation;

            textAlignment = (TextAlignmentOptions)data.textAlignment;
            useThemedBox = data.useThemedBox;
            textColorHasSet = data.textColorHasSet;
            textColor = data.textColor;
            materialName = data.materialName;

            NewPage();
            foreach (var displayData in data.displayDatas)
            {
                AppendDialogue(displayData, needAnimation: false);
            }

            if (data.closeButtonShown)
            {
                ShowCloseButton();
            }
            else
            {
                HideCloseButton();
            }

            dialogueFinishIconEnabled = data.dialogueFinishIconEnabled;
        }

        #endregion
    }
}