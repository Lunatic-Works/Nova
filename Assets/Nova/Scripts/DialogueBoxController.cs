using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nova
{
    public enum DialogueBoxState
    {
        Normal,
        Auto,
        Skip
    }

    public class DialogueBoxController : MonoBehaviour, IPointerClickHandler
    {
        public bool needAnimation;

        public float characterDisplayDuration;

        /// <summary>
        /// The regex expression to find the name
        /// </summary>
        public string namePattern;

        /// <summary>
        /// The group of the name in the name pattern
        /// </summary>
        public int nameGroup;

        private GameState gameState;

        private GameObject nameBox;
        private Text nameTextArea;
        private Text dialogueTextArea;

        private void Awake()
        {
            nameBox = transform.Find("NameBox").gameObject;
            nameTextArea = nameBox.transform.Find("Text").GetComponent<Text>();
            dialogueTextArea = transform.Find("DialogueBox/Text").GetComponent<Text>();

            gameState = Utils.FindNovaGameController().GetComponent<GameState>();
            gameState.DialogueWillChange += OnDialogueWillChange;
            gameState.DialogueChanged += OnDialogueChanged;
            gameState.BranchOccurs += OnBranchOcurrs;
            gameState.BranchSelected += OnBranchSelected;
            gameState.CurrentRouteEnded += OnCurrentRouteEnded;
        }

        private void OnCurrentRouteEnded(CurrentRouteEndedData arg0)
        {
            State = DialogueBoxState.Normal;
        }

        private void OnDialogueWillChange()
        {
            StopTimer();
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            gameState.DialogueWillChange -= OnDialogueWillChange;
            gameState.DialogueChanged -= OnDialogueChanged;
            gameState.BranchOccurs -= OnBranchOcurrs;
            gameState.BranchSelected -= OnBranchSelected;
            gameState.CurrentRouteEnded -= OnCurrentRouteEnded;
        }

        private string currentName;
        private string currentDialogue;

        private Coroutine animationCoroutine;

        private DialogueBoxState _state = DialogueBoxState.Normal;

        /// <summary>
        /// Current state of the dialogue box
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public DialogueBoxState State
        {
            get { return _state; }
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
                        AutoModeStops.Invoke();
                        break;
                    case DialogueBoxState.Skip:
                        StopSkip();
                        SkipModeStops.Invoke();
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
                        BeginAuto();
                        AutoModeStarts.Invoke();
                        break;
                    case DialogueBoxState.Skip:
                        BeginSkip();
                        SkipModeStarts.Invoke();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("value", value, null);
                }
            }
        }

        public UnityEvent AutoModeStarts;
        public UnityEvent AutoModeStops;
        public UnityEvent SkipModeStarts;
        public UnityEvent SkipModeStops;

        private bool isAnimating;

        /// <summary>
        /// The content of the dialogue box needs to be changed
        /// </summary>
        /// <param name="text"></param>
        private void OnDialogueChanged(DialogueChangedData dialogueData)
        {
            RestartTimer();
            var text = dialogueData.text;
            Debug.Log(string.Format("<color=green><b>{0}</b></color>", text));

            // Parse dialogue text
            ParseDialogueText(text);

            // Change display
            ChangeDisplay();

            // Check current state and set schedule skip
            SetSchedule();
        }

        private void SetSchedule()
        {
            TryRemoveSchedule();
            switch (State)
            {
                case DialogueBoxState.Normal:
                    break;
                case DialogueBoxState.Auto:
                    TrySchedule(GetAutoScheduledTime());
                    break;
                case DialogueBoxState.Skip:
                    TrySchedule(SkipDelay);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Change display based on current name and current dialogue
        /// </summary>
        private void ChangeDisplay()
        {
            if (currentName == "")
            {
                nameBox.SetActive(false);
            }
            else
            {
                nameBox.SetActive(true);
                nameTextArea.text = currentName;
            }

            if (!needAnimation)
            {
                dialogueTextArea.text = currentDialogue;
                return;
            }

            // Need animantion
            if (isAnimating)
            {
                StopCharacterAnimation();
            }

            StartCharacterAnimation();
        }

        /// <summary>
        /// Set current name and current dialogue based on dialogue text
        /// </summary>
        /// <param name="text">the dialogue text</param>
        private void ParseDialogueText(string text)
        {
            var m = Regex.Match(text, namePattern);
            var dialogueStartIndex = 0;
            if (m.Success)
            {
                currentName = m.Groups[nameGroup].Value;
                dialogueStartIndex = m.Length;
            }
            else // No name is found
            {
                currentName = "";
            }

            currentDialogue = text.Substring(dialogueStartIndex).Trim();
        }

        private DialogueBoxState stateBeforeBranch;

        /// <summary>
        /// Make the state normal when branch occurs
        /// </summary>
        /// <param name="branchOccursData"></param>
        private void OnBranchOcurrs(BranchOccursData branchOccursData)
        {
            stateBeforeBranch = State;
            State = DialogueBoxState.Normal;
        }

        public bool continueAutoAfterBranch;
        public bool continueSkipAfterBranch;

        /// <summary>
        /// Check if should restore the previous state before the branch happens
        /// </summary>
        /// <param name="branchSelectedData"></param>
        private void OnBranchSelected(BranchSelectedData branchSelectedData)
        {
            Assert.AreEqual(State, DialogueBoxState.Normal, "DialogueBoxState.Normal != DialogueBox.State");
            switch (stateBeforeBranch)
            {
                case DialogueBoxState.Normal:
                    break;
                case DialogueBoxState.Auto:
                    State = continueAutoAfterBranch ? DialogueBoxState.Auto : DialogueBoxState.Normal;
                    break;
                case DialogueBoxState.Skip:
                    State = continueSkipAfterBranch ? DialogueBoxState.Skip : DialogueBoxState.Normal;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void StartCharacterAnimation()
        {
            Assert.AreEqual(isAnimating, false,
                "Start character animation should be called when previous animation is stoped");
            isAnimating = true;
            animationCoroutine = StartCoroutine(CharacterAnimation());
        }

        /// <summary>
        /// Stop the current animation
        /// </summary>
        private void StopCharacterAnimation()
        {
            if (!isAnimating)
            {
                return;
            }

            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }

            isAnimating = false;
            dialogueTextArea.text = currentDialogue;
        }

        /// <summary>
        /// Use coroutine to play animation, display the character one by one
        /// </summary>
        /// <returns></returns>
        private IEnumerator CharacterAnimation()
        {
            // empty text Area to handle the case when the dialogue is empty
            dialogueTextArea.text = "";
            for (var index = 1; index <= currentDialogue.Length; ++index)
            {
                dialogueTextArea.text = currentDialogue.Substring(0, index);
                yield return new WaitForSeconds(characterDisplayDuration);
            }

            // Animation stop
            isAnimating = false;
        }

        private Coroutine scheduledStepCoroutine = null;

        public float AutoWaitTimePerCharacter;


        private void TrySchedule(float scheduledDelay)
        {
            if (dialogueAvaliable)
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

        private float GetAutoScheduledTime()
        {
            return AutoWaitTimePerCharacter * currentDialogue.Length;
        }

        /// <summary>
        /// Start auto
        /// </summary>
        /// <remarks>
        /// This method should be called when the state is normal
        /// </remarks>
        private void BeginAuto()
        {
            Assert.AreEqual(State, DialogueBoxState.Normal, "DialogueBoxState State != DialogueBoxState.Normal");
            _state = DialogueBoxState.Auto;
            TrySchedule(GetAutoScheduledTime());
        }


        /// <summary>
        /// Stop Auto
        /// </summary>
        /// <remarks>
        /// This method should be called when the state is auto
        /// </remarks>
        private void StopAuto()
        {
            Assert.AreEqual(State, DialogueBoxState.Auto, "DialogueBoxState State != DialogueBoxState.Auto");
            _state = DialogueBoxState.Normal;
            TryRemoveSchedule();
        }

        public float SkipDelay;
        private bool shouldNeedAnimation;


        /// <summary>
        /// Begin skip
        /// </summary>
        /// <remarks>
        /// This method should be called when the state is normal
        /// </remarks>
        private void BeginSkip()
        {
            Assert.AreEqual(State, DialogueBoxState.Normal, "DialogueBoxState State != DialogueBoxState.Normal");
            // Stop character animation
            StopCharacterAnimation();
            shouldNeedAnimation = needAnimation;
            needAnimation = false;
            _state = DialogueBoxState.Skip;
            TrySchedule(SkipDelay);
        }

        /// <summary>
        /// Stop skip
        /// </summary>
        /// <remarks>
        /// This method should be called when the state is Skip
        /// </remarks>
        private void StopSkip()
        {
            Assert.AreEqual(State, DialogueBoxState.Skip, "DialogueBoxState State != DialogueBoxState.Skip");
            needAnimation = shouldNeedAnimation;
            _state = DialogueBoxState.Normal;
            TryRemoveSchedule();
        }

        private IEnumerator ScheduledStep(float scheduledDelay)
        {
            Assert.IsTrue(dialogueAvaliable, "Dialogue should available when a step scheduled for it");
            while (scheduledDelay > timeAfterDialogueChange)
            {
                yield return new WaitForSeconds(scheduledDelay - timeAfterDialogueChange);
            }

            // Pause one frame before step
            // Give time for rendering and can stop schedule step in time before any unwanted effects occurs
            yield return null;

            if (gameState.canStepForward)
            {
                gameState.Step();
            }
            else
            {
                State = DialogueBoxState.Normal;
            }
        }

        private float timeAfterDialogueChange;

        private bool dialogueAvaliable;

        private void StopTimer()
        {
            timeAfterDialogueChange = 0;
            dialogueAvaliable = false;
        }

        private void RestartTimer()
        {
            timeAfterDialogueChange = 0;
            dialogueAvaliable = true;
        }

        private void Update()
        {
            if (dialogueAvaliable)
            {
                timeAfterDialogueChange += Time.deltaTime;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (State == DialogueBoxState.Normal && !isAnimating)
            {
                gameState.Step();
                return;
            }

            if (isAnimating)
            {
                StopCharacterAnimation();
            }

            State = DialogueBoxState.Normal;
        }
    }
}