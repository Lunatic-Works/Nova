using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
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
        private VerticalLayoutGroup dialogueTextVerticalLayoutGroup;

        private ButtonRingTrigger buttonRingTrigger;
        private SaveViewController saveViewController;
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
                        scrollRectTransform.offsetMin = new Vector2(120f, 0f);
                        scrollRectTransform.offsetMax = new Vector2(-180f, -40f);
                        dialogueTextVerticalLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
                        dialogueEntryLayoutSetting = new DialogueEntryLayoutSetting
                        {
                            leftPadding = 0,
                            rightPadding = 0,
                            nameTextSpacing = 16f,
                            preferredHeight = 180f
                        };
                        break;
                    case Theme.Basic:
                        scrollRectTransform.offsetMin = new Vector2(60f, 42f);
                        scrollRectTransform.offsetMax = new Vector2(-120f, -42f);
                        dialogueTextVerticalLayoutGroup.padding = new RectOffset(0, 0, 0, 120);
                        dialogueEntryLayoutSetting = DialogueEntryLayoutSetting.Default;
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

        protected override bool Init()
        {
            if (base.Init())
            {
                return true;
            }

            gameController = Utils.FindNovaGameController();
            gameState = gameController.GameState;
            configManager = gameController.ConfigManager;

            dialogueTextScrollRect = GetComponentInChildren<ScrollRect>();
            dialogueText = GetComponentInChildren<DialogueTextController>();
            dialogueTextRect = dialogueText.transform as RectTransform;
            dialogueTextVerticalLayoutGroup = dialogueText.GetComponent<VerticalLayoutGroup>();

            buttonRingTrigger = GetComponentInChildren<ButtonRingTrigger>();
            saveViewController = viewManager.GetController<SaveViewController>();
            logController = viewManager.GetController<LogController>();

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

            return false;
        }

        protected override void Awake()
        {
            base.Awake();
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
            gameState.dialogueWillChange.AddListener(OnDialogueWillChange);
            gameState.dialogueChanged.AddListener(OnDialogueChanged);
            gameState.routeEnded.AddListener(OnRouteEnded);
            gameState.AddRestorable(this);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            gameState.dialogueWillChange.RemoveListener(OnDialogueWillChange);
            gameState.dialogueChanged.RemoveListener(OnDialogueChanged);
            gameState.routeEnded.RemoveListener(OnRouteEnded);
            gameState.RemoveRestorable(this);
        }

        private void OnRouteEnded(RouteEndedData routeEndedData)
        {
            state = DialogueBoxState.Normal;
            this.SwitchView<TitleController>();
        }

        private void OnDialogueWillChange(DialogueWillChangeData dialogueWillChangeData)
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

            if (gameController.inputEnabled)
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

        [SerializeField] private GameObject autoModeIcon;
        [SerializeField] private GameObject fastForwardModeIcon;
        [SerializeField] private Material fastForwardPostProcessingMaterial;

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
                            int clicks = configManager.GetInt(FastForwardReadFirstShownKey);
                            if (clicks < HintFastForwardReadClicks)
                            {
                                Alert.Show(I18n.__("dialogue.noreadtext"));
                                configManager.SetInt(FastForwardReadFirstShownKey, clicks + 1);
                            }
                            else if (clicks == HintFastForwardReadClicks)
                            {
                                Alert.Show(I18n.__("dialogue.hint.fastforwardread"));
                                configManager.SetInt(FastForwardReadFirstShownKey, clicks + 1);
                            }
                            else
                            {
                                Alert.Show(I18n.__("dialogue.noreadtext"));
                            }

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

        /// <summary>
        /// The content of the dialogue box needs to be changed
        /// </summary>
        /// <param name="dialogueData"></param>
        private void OnDialogueChanged(DialogueChangedData dialogueData)
        {
            RestartTimer();

            isReadDialogue = dialogueData.isReachedAnyHistory;
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

        // dialogueTextScrollRect.verticalNormalizedPosition cannot be out of [0, 1],
        // and it may be out of sync with dialogueTextRect.localPosition.y when the content changes
        public void SetTextScroll(float value)
        {
            var position = dialogueTextRect.localPosition;
            dialogueTextRect.localPosition = new Vector3(position.x, value, position.z);
        }

        public void ResetTextScroll()
        {
            dialogueTextScrollRect.verticalNormalizedPosition = 0f;
        }

        public RectTransformAnimationProperty GetTextScrollAnimationProperty(float start, float target)
        {
            var x = dialogueTextRect.localPosition.x;
            return new RectTransformAnimationProperty(dialogueTextRect, new Vector2(x, start), new Vector2(x, target),
                true);
        }

        [HideInInspector] public float perCharacterFadeInDuration;

        private void AppendDialogue(DialogueDisplayData displayData, bool needAnimation = true)
        {
            Color nowTextColor = textColorHasSet ? textColor : isReadDialogue ? readColor : unreadColor;
            textLeftExtraPadding = avatarController.textPaddingOrZero;
            var entry = dialogueText.AddEntry(displayData, textAlignment, nowTextColor, nowTextColor, materialName,
                dialogueEntryLayoutSetting, textLeftExtraPadding);

            if (this.needAnimation && needAnimation && !gameState.isRestoring && state != DialogueBoxState.FastForward)
            {
                var contentProxy = entry.contentProxy;

                float textDuration;
                if (textDurationOverride >= 0f)
                {
                    textDuration = textDurationOverride;
                }
                else
                {
                    textDuration = perCharacterFadeInDuration * contentProxy.GetPageCharacterCount();
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
                    ResetTextScroll();
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
                CharacterController.MaxVoiceDurationNextDialogue + voiceOffset
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
            this.RuntimeAssert(state == DialogueBoxState.Normal, "Dialogue box state != Normal");
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
            this.RuntimeAssert(state == DialogueBoxState.Auto, "Dialogue box state != Auto");
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
            this.RuntimeAssert(state == DialogueBoxState.Normal, "Dialogue box state != Normal");
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
            this.RuntimeAssert(state == DialogueBoxState.FastForward, "Dialogue box state != FastForward");
            _state = DialogueBoxState.Normal;
            TryRemoveSchedule();
        }

        private bool NextPageOrStep()
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

        [SerializeField] private Color readColor;
        [SerializeField] private Color unreadColor;

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

        private DialogueEntryLayoutSetting _dialogueEntryLayoutSetting = DialogueEntryLayoutSetting.Default;

        // Modified only by theme
        private DialogueEntryLayoutSetting dialogueEntryLayoutSetting
        {
            get => _dialogueEntryLayoutSetting;
            set
            {
                _dialogueEntryLayoutSetting = value;
                foreach (var dec in dialogueText.dialogueEntryControllers)
                {
                    dec.layoutSetting = value;
                }
            }
        }

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
            public readonly Theme theme;
            public readonly int textAlignment;
            public readonly bool textColorHasSet;
            public readonly Vector4Data textColor;
            public readonly string materialName;
            public readonly bool closeButtonShown;
            public readonly bool dialogueFinishIconShown;

            public DialogueBoxRestoreData(RectTransform rect, Color backgroundColor,
                DialogueUpdateMode dialogueUpdateMode, List<DialogueDisplayData> displayDatas, bool canClickForward,
                bool scriptCanAbortAnimation, Theme theme, int textAlignment, bool textColorHasSet,
                Color textColor, string materialName, bool closeButtonShown, bool dialogueFinishIconShown)
            {
                rectTransformRestoreData = new RectTransformRestoreData(rect);
                this.backgroundColor = backgroundColor;
                this.dialogueUpdateMode = dialogueUpdateMode;
                this.displayDatas = displayDatas;
                this.canClickForward = canClickForward;
                this.scriptCanAbortAnimation = scriptCanAbortAnimation;
                this.theme = theme;
                this.textAlignment = textAlignment;
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
                canClickForward, scriptCanAbortAnimation, theme, (int)textAlignment, textColorHasSet,
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

            theme = data.theme;
            textAlignment = (TextAlignmentOptions)data.textAlignment;
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