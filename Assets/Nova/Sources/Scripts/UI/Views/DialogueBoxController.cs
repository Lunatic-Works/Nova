using System;
using System.Collections;
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
    public class DialogueBoxController : ViewControllerBase, IRestorable
    {
        [ExportCustomType]
        public enum DialogueUpdateMode
        {
            Overwrite,
            Append
        }

        [HideInInspector] public DialogueUpdateMode dialogueUpdateMode;

        private GameState gameState;
        private DialogueState dialogueState;

        private ScrollRect dialogueTextScrollRect;
        private DialogueTextController dialogueText;
        private RectTransform dialogueTextRect;
        private VerticalLayoutGroup dialogueTextVerticalLayoutGroup;

        private AvatarController avatarController;

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
        private readonly List<CanvasGroup> backgroundCanvasGroups = new List<CanvasGroup>();
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

            var controller = Utils.FindNovaController();
            gameState = controller.GameState;
            dialogueState = controller.DialogueState;

            dialogueTextScrollRect = GetComponentInChildren<ScrollRect>();
            dialogueText = GetComponentInChildren<DialogueTextController>();
            dialogueTextRect = dialogueText.transform as RectTransform;
            dialogueTextVerticalLayoutGroup = dialogueText.GetComponent<VerticalLayoutGroup>();

            avatarController = GetComponentInChildren<AvatarController>();

            rect = transform.Find("DialoguePanel").GetComponent<RectTransform>();

            foreach (var image in backgroundImages)
            {
                backgroundCanvasGroups.Add(image.GetComponent<CanvasGroup>());
            }

            foreach (var btn in hideDialogueButtons)
            {
                btn.onClick.AddListener(Hide);
            }

            LuaRuntime.Instance.BindObject("dialogueBoxController", this);
            gameState.AddRestorable(this);

            return false;
        }

        protected override void OnDestroy()
        {
            gameState.RemoveRestorable(this);

            base.OnDestroy();
        }

        private void OnEnable()
        {
            gameState.dialogueWillChange.AddListener(OnDialogueWillChange);
            gameState.dialogueChanged.AddListener(OnDialogueChanged);
            gameState.routeEnded.AddListener(OnRouteEnded);

            dialogueState.autoModeStarts.AddListener(OnAutoModeStarts);
            dialogueState.autoModeStops.AddListener(OnAutoModeStops);
            dialogueState.fastForwardModeStarts.AddListener(OnFastForwardModeStarts);
            dialogueState.fastForwardModeStops.AddListener(OnFastForwardModeStops);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            dialogueState.state = DialogueState.State.Normal;

            gameState.dialogueWillChange.RemoveListener(OnDialogueWillChange);
            gameState.dialogueChanged.RemoveListener(OnDialogueChanged);
            gameState.routeEnded.RemoveListener(OnRouteEnded);

            dialogueState.autoModeStarts.RemoveListener(OnAutoModeStarts);
            dialogueState.autoModeStops.RemoveListener(OnAutoModeStops);
            dialogueState.fastForwardModeStarts.RemoveListener(OnFastForwardModeStarts);
            dialogueState.fastForwardModeStops.RemoveListener(OnFastForwardModeStops);
        }

        private void OnRouteEnded(RouteEndedData routeEndedData)
        {
            dialogueState.state = DialogueState.State.Normal;
            this.SwitchView<TitleController>();
        }

        private void OnDialogueWillChange()
        {
            StopTimer();
            ResetTextAnimationConfig();
            ShowDialogueFinishIcon(false);
        }

        protected override void Update()
        {
            if (viewManager.currentView == CurrentViewType.Game && dialogueAvailable)
            {
                timeAfterDialogueChange += Time.deltaTime;

                if (dialogueFinishIconShown && dialogueState.isNormal &&
                    viewManager.currentView != CurrentViewType.InTransition && timeAfterDialogueChange > dialogueTime)
                {
                    ShowDialogueFinishIcon(true);
                }
            }
        }

        // Used when aborting animations by clicking
        public void ShowDialogueFinishIcon(bool to)
        {
            foreach (var icon in dialogueFinishIcons)
            {
                icon.SetActive(to);
            }
        }

        [SerializeField] private GameObject autoModeIcon;
        [SerializeField] private GameObject fastForwardModeIcon;

        /// <summary>
        /// The content of the dialogue box needs to be changed
        /// </summary>
        /// <param name="data"></param>
        private void OnDialogueChanged(DialogueChangedData data)
        {
            RestartTimer();

            switch (dialogueUpdateMode)
            {
                case DialogueUpdateMode.Overwrite:
                    OverwriteDialogue(data.displayData);
                    break;
                case DialogueUpdateMode.Append:
                    AppendDialogue(data.displayData);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            SetSchedule();
            dialogueTime = GetDialogueTime();
        }

        // Check current state and set schedule for the next dialogue entry
        private void SetSchedule()
        {
            TryRemoveSchedule();
            switch (dialogueState.state)
            {
                case DialogueState.State.Normal:
                    break;
                case DialogueState.State.Auto:
                    TrySchedule(GetDialogueTimeAuto());
                    break;
                case DialogueState.State.FastForward:
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

        [HideInInspector] public float characterFadeInDuration;

        private void AppendDialogue(DialogueDisplayData displayData, bool needAnimation = true)
        {
            Color nowTextColor = textColorHasSet ? textColor : dialogueState.isReadDialogue ? readColor : unreadColor;
            textLeftExtraPadding = avatarController.textPaddingOrZero;
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

        [HideInInspector] public float autoDelay;
        [HideInInspector] public float fastForwardDelay;

        private Coroutine scheduledStepCoroutine;

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
                GameCharacterController.MaxVoiceDuration + voiceOffset
            );
        }

        private float GetDialogueTimeAuto()
        {
            return Mathf.Max(
                GetDialogueTime(autoDelay, autoDelay * 0.5f),
                NovaAnimation.GetTotalTimeRemaining(AnimationType.Text) / characterFadeInDuration * autoDelay * 0.1f
            );
        }

        private void OnAutoModeStarts()
        {
            TrySchedule(GetDialogueTimeAuto());

            if (autoModeIcon != null)
            {
                autoModeIcon.SetActive(true);
            }
        }

        private void OnAutoModeStops()
        {
            TryRemoveSchedule();

            if (autoModeIcon != null)
            {
                autoModeIcon.SetActive(false);
            }
        }

        private void OnFastForwardModeStarts()
        {
            TrySchedule(fastForwardDelay);

            if (fastForwardModeIcon != null)
            {
                fastForwardModeIcon.SetActive(true);
            }
        }

        private void OnFastForwardModeStops()
        {
            TryRemoveSchedule();

            if (fastForwardModeIcon != null)
            {
                fastForwardModeIcon.SetActive(false);
            }
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

        private IEnumerator ScheduledStep(float scheduledDelay)
        {
            this.RuntimeAssert(dialogueAvailable, "Dialogue not available when scheduling a step for it.");

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
                    TrySchedule(dialogueState.isAuto ? autoDelay : fastForwardDelay);
                }
            }
            else
            {
                dialogueState.state = DialogueState.State.Normal;
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
                foreach (var btn in hideDialogueButtons)
                {
                    btn.gameObject.SetActive(value);
                }
            }
        }

        [HideInInspector] public bool dialogueFinishIconShown = true;

        #endregion

        #region Restoration

        public string restorableName => "DialogueBoxController";

        [Serializable]
        private class DialogueBoxControllerRestoreData : IRestoreData
        {
            public readonly RectTransformData rectTransformData;
            public readonly Vector4Data backgroundColor;
            public readonly DialogueUpdateMode dialogueUpdateMode;
            public readonly List<DialogueDisplayData> displayDatas;
            public readonly Theme theme;
            public readonly int textAlignment;
            public readonly bool textColorHasSet;
            public readonly Vector4Data textColor;
            public readonly string materialName;
            public readonly bool closeButtonShown;
            public readonly bool dialogueFinishIconShown;

            public DialogueBoxControllerRestoreData(RectTransform rect, Color backgroundColor,
                DialogueUpdateMode dialogueUpdateMode, List<DialogueDisplayData> displayDatas, Theme theme,
                int textAlignment, bool textColorHasSet, Color textColor, string materialName, bool closeButtonShown,
                bool dialogueFinishIconShown)
            {
                rectTransformData = new RectTransformData(rect);
                this.backgroundColor = backgroundColor;
                this.dialogueUpdateMode = dialogueUpdateMode;
                this.displayDatas = displayDatas;
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
            return new DialogueBoxControllerRestoreData(rect, backgroundColor, dialogueUpdateMode, displayDatas, theme,
                (int)textAlignment, textColorHasSet, textColor, materialName, closeButtonShown,
                dialogueFinishIconShown);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as DialogueBoxControllerRestoreData;
            data.rectTransformData.Restore(rect);
            backgroundColor = data.backgroundColor;

            dialogueUpdateMode = data.dialogueUpdateMode;

            theme = data.theme;
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
