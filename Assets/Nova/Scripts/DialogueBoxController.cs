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
            gameState.DialogueChanged += OnDialogueChanged;
            gameState.BranchOccurs += OnBranchOcurrs;
            gameState.BranchSelected += OnBranchSelected;
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            gameState.DialogueChanged -= OnDialogueChanged;
            gameState.BranchOccurs -= OnBranchOcurrs;
            gameState.BranchSelected -= OnBranchSelected;
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
                        break;
                    case DialogueBoxState.Skip:
                        StopSkip();
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
                        break;
                    case DialogueBoxState.Skip:
                        BeginSkip();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("value", value, null);
                }
            }
        }

        private bool isAnimating;

        /// <summary>
        /// The content of the dialogue box needs to be changed
        /// </summary>
        /// <param name="text"></param>
        private void OnDialogueChanged(DialogueChangedData dialogueData)
        {
            var text = dialogueData.text;
            Debug.Log(string.Format("<color=green><b>{0}</b></color>", text));

            // Parse dialogue text
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

            // Change display
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

        private DialogueBoxState stateBeforeBranch;

        /// <summary>
        /// Make the state normal when branch occurs
        /// </summary>
        /// <param name="branchOccursData"></param>
        private void OnBranchOcurrs(BranchOccursData branchOccursData)
        {
            stateBeforeBranch = State;
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

        private Coroutine autoCoroutine = null;

        public float AutoWaitTimePerCharacter;

        public UnityEvent AutoModeStarts;

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
            AutoModeStarts.Invoke();
            // Make sure no one stop auto mode after immediately recieves an AutoModeStarts event
            // There is no need to worry about BeginAuto method called again when this event happens, since
            // This is a private method called by the setter of State
            if (_state == DialogueBoxState.Auto)
            {
                autoCoroutine = StartCoroutine(Auto());
            }
        }

        public UnityEvent AutoModeStops;

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
            // The if condition here is to make sure no error will be raised if someone switch the State immediately
            // after receives AutoModeStarts event
            if (autoCoroutine != null)
            {
                StopCoroutine(autoCoroutine);
                autoCoroutine = null;
            }

            AutoModeStops.Invoke();
        }

        private IEnumerator Auto()
        {
            while (gameState.canStepForward)
            {
                if (currentDialogue == null)
                {
                    Debug.LogError("current dialogue not set, Auto mode stop");
                    break;
                }

                yield return new WaitForSeconds(currentDialogue.Length * AutoWaitTimePerCharacter);
                gameState.Step();
            }

            _state = DialogueBoxState.Normal;
            AutoModeStops.Invoke();
        }

        private Coroutine skipCoroutine = null;
        private float SkipDuration;
        private bool shouldNeedAnimation;

        public UnityEvent SkipModeStarts;

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
            SkipModeStarts.Invoke();
            // Make sure no one stop skip mode after immediately recieves an SkipModeStarts event
            // There is no need to worry about BeginSkip method called again when this event happens, since
            // This is a private method called by the setter of State
            if (_state == DialogueBoxState.Skip)
            {
                skipCoroutine = StartCoroutine(Skip());
            }
        }

        public UnityEvent SkipModeStops;

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
            // The if condition here is to make sure no error will be raised if someone switch the State immediately
            // after receives SkipModeStarts event
            if (skipCoroutine != null)
            {
                StopCoroutine(skipCoroutine);
                skipCoroutine = null;
            }

            SkipModeStops.Invoke();
        }

        private IEnumerator Skip()
        {
            while (gameState.canStepForward)
            {
                if (currentDialogue == null)
                {
                    Debug.LogError("current dialogue not set, Skip mode stop");
                    break;
                }

                gameState.Step();
                yield return new WaitForSeconds(SkipDuration);
            }

            _state = DialogueBoxState.Normal;
            needAnimation = shouldNeedAnimation;
            SkipModeStops.Invoke();
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