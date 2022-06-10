using System;
using UnityEngine;
using UnityEngine.Events;

namespace Nova
{
    [ExportCustomType]
    public class DialogueState : MonoBehaviour
    {
        private const string FastForwardReadFirstShownKey = ConfigManager.FirstShownKeyPrefix + "FastForwardRead";
        private const int HintFastForwardReadClicks = 3;

        private GameState gameState;
        private ConfigManager configManager;

        private void Awake()
        {
            var gameController = Utils.FindNovaGameController();
            gameState = gameController.GameState;
            configManager = gameController.ConfigManager;

            LuaRuntime.Instance.BindObject("dialogueState", this);
            gameState.dialogueChangedEarly.AddListener(OnDialogueChangedEarly);
        }

        private void OnDestroy()
        {
            gameState.dialogueChangedEarly.RemoveListener(OnDialogueChangedEarly);
        }

        [ExportCustomType]
        public enum State
        {
            Normal,
            Auto,
            FastForward
        }

        private State _state = State.Normal;

        public State state
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
                    case State.Normal:
                        break;
                    case State.Auto:
                        autoModeStops.Invoke();
                        break;
                    case State.FastForward:
                        fastForwardModeStops.Invoke();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                switch (value)
                {
                    case State.Normal:
                        _state = State.Normal;
                        break;
                    case State.Auto:
                        this.RuntimeAssert(state == State.Normal, "Dialogue state is not Normal when setting to Auto");
                        _state = State.Auto;
                        autoModeStarts.Invoke();
                        break;
                    case State.FastForward:
                        this.RuntimeAssert(state == State.Normal,
                            "Dialogue state is not Normal when setting to FastForward");

                        if (unreadStopsFastForward)
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

                        _state = State.FastForward;
                        fastForwardModeStarts.Invoke();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool isNormal => state == State.Normal;
        public bool isAuto => state == State.Auto;
        public bool isFastForward => state == State.FastForward;

        public UnityEvent autoModeStarts;
        public UnityEvent autoModeStops;
        public UnityEvent fastForwardModeStarts;
        public UnityEvent fastForwardModeStops;

        [HideInInspector] public bool isReadDialogue;
        [HideInInspector] public bool onlyFastForwardRead;

        private bool _fastForwardHotKeyHolding;

        public bool fastForwardHotKeyHolding
        {
            get => _fastForwardHotKeyHolding;
            set
            {
                if (_fastForwardHotKeyHolding == value)
                {
                    return;
                }

                _fastForwardHotKeyHolding = value;
                state = value ? State.FastForward : State.Normal;
            }
        }

        private bool unreadStopsFastForward => !isReadDialogue && onlyFastForwardRead && !fastForwardHotKeyHolding;

        // Update state and isReadDialogue before OnDialogueChanged is invoked
        private void OnDialogueChangedEarly(DialogueChangedData dialogueData)
        {
            isReadDialogue = dialogueData.isReachedAnyHistory;

            if (isFastForward && unreadStopsFastForward)
            {
                state = State.Normal;
            }

            if (isFastForward)
            {
                NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
            }
        }
    }
}