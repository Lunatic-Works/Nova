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
        IRestorable
    {
        [ExportCustomType]
        public enum DialogueUpdateMode
        {
            Overwrite,
            Append
        }

        [HideInInspector] public DialogueUpdateMode dialogueUpdateMode;

        private GameController gameController;
        private GameState gameState;
        private ConfigManager configManager;

        private ScrollRect dialogueTextScrollRect;
        private DialogueTextController dialogueText;
        private RectTransform dialogueTextRect;

        private ButtonRingTrigger buttonRingTrigger;
        private LogController logController;
        [SerializeField] private AvatarController avatarController;

        // TODO: there are a lot of magic numbers for the current UI
        [ExportCustomType]
        public enum Theme
        {
            Default,
            Basic
        }

        private bool themeInited;
        private Theme _theme;

        public Theme theme
        {
            get => _theme;
            set
            {
                if (themeInited && _theme == value)
                {
                    return;
                }

                themeInited = true;
                _theme = value;
                Init();

                defaultBackgroundGO.SetActive(value == Theme.Default);
                basicBackgroundGO.SetActive(value == Theme.Basic);

                var scrollRectTransform = dialogueTextScrollRect.transform as RectTransform;

                switch (value)
                {
                    case Theme.Default:
                        scrollRectTransform.offsetMin = new Vector2(60f, 0f);
                        scrollRectTransform.offsetMax = new Vector2(-120f, -40f);
                        dialogueText.layoutSetting = new DialogueEntryLayoutSetting
                        {
                            leftPadding = 150,
                            rightPadding = 0,
                            nameTextSpacing = 16f,
                            preferredHeight = 150f
                        };
                        break;
                    case Theme.Basic:
                        scrollRectTransform.offsetMin = new Vector2(60f, 120f);
                        scrollRectTransform.offsetMax = new Vector2(-120f, -40f);
                        dialogueText.layoutSetting = DialogueEntryLayoutSetting.Default;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public RectTransform rect { get; private set; }

        [SerializeField] private GameObject defaultBackgroundGO;
        [SerializeField] private GameObject basicBackgroundGO;

        [SerializeField] private List<Image> backgroundImages;
        private List<CanvasGroup> backgroundCanvasGroups = new List<CanvasGroup>();
        [SerializeField] private List<Button> hideDialogueButtons;
        [SerializeField] private List<GameObject> dialogueFinishIcons;

        private Color _backgroundColor;

        public Color backgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                Init();

                foreach (var image in backgroundImages)
                {
                    image.color = new Color(_backgroundColor.r, _backgroundColor.g, _backgroundColor.b, 1f);
                }

                foreach (var cg in backgroundCanvasGroups)
                {
                    cg.alpha = _backgroundColor.a * configOpacity;
                }
            }
        }

        private float _configOpacity;

        public float configOpacity
        {
            get => _configOpacity;
            set
            {
                _configOpacity = value;
                Init();

                foreach (var cg in backgroundCanvasGroups)
                {
                    cg.alpha = backgroundColor.a * _configOpacity;
                }
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

            dialogueTextScrollRect = GetComponentInChildren<ScrollRect>();
            dialogueText = GetComponentInChildren<DialogueTextController>();
            dialogueTextRect = dialogueText.transform as RectTransform;

            buttonRingTrigger = GetComponentInChildren<ButtonRingTrigger>();
            logController = transform.parent.GetComponentInChildren<LogController>();

            rect = transform.Find("DialoguePanel").GetComponent<RectTransform>();

            foreach (var image in backgroundImages)
            {
                backgroundCanvasGroups.Add(image.GetComponent<CanvasGroup>());
            }

            foreach (var btn in hideDialogueButtons)
            {
                btn.onClick.AddListener(Hide);
            }

            dialogueFinished = new AndGate(to =>
            {
                foreach (var icon in dialogueFinishIcons)
                {
                    icon.SetActive(to);
                }
            });
            dialogueBoxShown = new AndGate(dialogueFinished);
            dialogueBoxShown.SetActive(true);

            uiPP = UICameraHelper.Active.GetComponent<PostProcessing>();

            LuaRuntime.Instance.BindObject("dialogueBoxController", this);

            inited = true;
        }

        protected override void Awake()
        {
            base.Awake();
            Init();
        }

        public override void Show(Action onFinish)
        {
            base.Show(onFinish);
            dialogueBoxShown.SetActive(true);
        }

        public override void Hide(Action onFinish)
        {
            state = DialogueBoxState.Normal;
            base.Hide(onFinish);
            dialogueBoxShown.SetActive(false);
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

        // Avoid refreshing text proxy when changing size in animation
        [HideInInspector] public bool canRefreshTextProxy = true;

        protected override void Update()
        {
            if (viewManager.currentView == CurrentViewType.Game && dialogueAvailable)
            {
                timeAfterDialogueChange += Time.deltaTime;

                if (dialogueFinishIconShown &&
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
            if (canRefreshTextProxy && rect.hasChanged)
            {
                // Debug.Log("Dialogue box size changed");

                foreach (var dec in dialogueText.dialogueEntryControllers)
                {
                    dec.ScheduleRefresh();
                }

                rect.hasChanged = false;
            }
        }

        public GameObject autoModeIcon;
        public GameObject fastForwardModeIcon;
        public Material fastForwardPostProcessingMaterial;

        private AndGate dialogueFinished;
        private AndGate dialogueBoxShown;
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

            isReadDialogue = dialogueData.hasBeenReachedForAnyVariables;
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
            dialogueTextScrollRect.verticalNormalizedPosition = 0f;
        }

        [SerializeField] private NovaAnimation textAnimation;
        [HideInInspector] public bool needAnimation = true;

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

        // ScrollRect.verticalNormalizedPosition cannot be out of [0, 1]
        public void SetTextScroll(float value)
        {
            var position = dialogueTextRect.localPosition;
            dialogueTextRect.localPosition = new Vector3(position.x, value, position.z);
        }

        public RectTransformAnimationProperty GetTextScrollAnimationProperty(float start, float target)
        {
            var x = dialogueTextRect.localPosition.x;
            return new RectTransformAnimationProperty(dialogueTextRect, new Vector2(x, start), new Vector2(x, target), true);
        }

        [HideInInspector] public float perCharacterFadeInDuration;

        private void AppendDialogue(DialogueDisplayData displayData, bool needAnimation = true)
        {
            Color nowTextColor = textColorHasSet ? textColor : isReadDialogue ? readColor : unreadColor;
            dialogueText.textLeftExtraPadding = avatarController.textPaddingOrZero;
            var entry = dialogueText.AddEntry(displayData, textAlignment, nowTextColor, nowTextColor, materialName);

            if (this.needAnimation && needAnimation && !gameState.isMovingBack && state != DialogueBoxState.FastForward)
            {
                var contentProxy = entry.contentProxy;

                float textDuration;
                if (textDurationOverride >= 0f)
                {
                    textDuration = textDurationOverride;
                }
                else
                {
                    var characterCount = contentProxy.textBox.GetTextInfo(contentProxy.text).characterCount;

                    // TODO: sometimes textInfo.characterCount returns 0, use text.Length
                    if (characterCount <= 0 && contentProxy.text.Length > 0)
                    {
                        Debug.LogWarning(
                            $"characterCount mismatch: {characterCount} {contentProxy.text.Length} {contentProxy.text}");
                        characterCount = contentProxy.text.Length;
                    }

                    textDuration = perCharacterFadeInDuration * characterCount;

                    // TMP has many strange behaviour, if the character animation looks strange, uncomment following
                    // lines to debug
                    // Debug.LogFormat("pc duration: {0}, char cnt: {1}, total duration: {2}, text: {3}",
                    //      PerCharacterFadeInDuration,
                    //      contentBox.textInfo.characterCount, textDuration, contentBox.text);
                }

                var animEntry = textAnimation.Do(new ActionAnimationProperty(() => contentProxy.SetTextAlpha(0))) // hide text
                    .Then(null).For(textAnimationDelay)
                    .Then(
                        new TextFadeInAnimationProperty(contentProxy, (byte)(255 * nowTextColor.a)),
                        textDuration
                    );
                if (!textScrollOverriden)
                {
                    animEntry.And(
                        new VerticalScrollRectAnimationProperty(dialogueTextScrollRect, 0f),
                        textDuration,
                        AnimationEntry.CubicEasing(0f, 1f)
                    );
                }
            }
            else
            {
                if (!textScrollOverriden)
                {
                    dialogueTextScrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }

        private void OverwriteDialogue(DialogueDisplayData displayData)
        {
            NewPage();
            AppendDialogue(displayData);
        }

        [HideInInspector] public float autoDelay;
        [HideInInspector] public float fastForwardDelay;

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
            if (dialogueText.dialogueEntryControllers.Count != 1 || !dialogueText.dialogueEntryControllers[0].Forward())
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
                    timeAfterDialogueChange = 0f;
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
            timeAfterDialogueChange = 0f;
            dialogueAvailable = false;
        }

        private void RestartTimer()
        {
            timeAfterDialogueChange = 0f;
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

                foreach (var btn in hideDialogueButtons)
                {
                    btn.gameObject.SetActive(value);
                }
            }
        }

        [HideInInspector] public bool dialogueFinishIconShown = true;

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
            public readonly Theme theme;
            public readonly bool textColorHasSet;
            public readonly Vector4Data textColor;
            public readonly string materialName;
            public readonly bool closeButtonShown;
            public readonly bool dialogueFinishIconShown;

            public DialogueBoxRestoreData(RectTransform rect, Color backgroundColor,
                DialogueUpdateMode dialogueUpdateMode, List<DialogueDisplayData> displayDatas, bool canClickForward,
                bool scriptCanAbortAnimation, int textAlignment, Theme theme, bool textColorHasSet,
                Color textColor, string materialName, bool closeButtonShown, bool dialogueFinishIconShown)
            {
                rectTransformRestoreData = new RectTransformRestoreData(rect);
                this.backgroundColor = backgroundColor;
                this.dialogueUpdateMode = dialogueUpdateMode;
                this.displayDatas = displayDatas;
                this.canClickForward = canClickForward;
                this.scriptCanAbortAnimation = scriptCanAbortAnimation;
                this.textAlignment = textAlignment;
                this.theme = theme;
                this.textColorHasSet = textColorHasSet;
                this.textColor = textColor;
                this.materialName = materialName;
                this.closeButtonShown = closeButtonShown;
                this.dialogueFinishIconShown = dialogueFinishIconShown;
            }
        }

        public IRestoreData GetRestoreData()
        {
            var displayDatas = dialogueText.dialogueEntryControllers.Select(x => x.displayData).ToList();
            return new DialogueBoxRestoreData(rect, backgroundColor, dialogueUpdateMode, displayDatas,
                canClickForward, scriptCanAbortAnimation, (int)textAlignment, theme, textColorHasSet,
                textColor, materialName, closeButtonShown, dialogueFinishIconShown);
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
            theme = data.theme;
            textColorHasSet = data.textColorHasSet;
            textColor = data.textColor;
            materialName = data.materialName;

            NewPage();
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