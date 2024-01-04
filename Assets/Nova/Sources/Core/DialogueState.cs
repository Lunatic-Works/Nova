using System;
using UnityEngine;
using UnityEngine.Events;

namespace Nova
{
    [ExportCustomType]
    public class DialogueState : MonoBehaviour
    {
        private const string FastForwardUnreadFirstShownKey = ConfigManager.FirstShownKeyPrefix + "FastForwardUnread";
        private const int HintFastForwardUnreadClicks = 3;

        private GameState gameState;
        private ConfigManager configManager;

        private void Awake()
        {
            var controller = Utils.FindNovaController();
            gameState = controller.GameState;
            configManager = controller.ConfigManager;

            LuaRuntime.Instance.BindObject("dialogueState", this);
            gameState.dialogueChangedEarly.AddListener(OnDialogueChangedEarly);
        }

        private void OnDestroy()
        {
            gameState.dialogueChangedEarly.RemoveListener(OnDialogueChangedEarly);
        }

        private bool stopFastForward => !isDialogueReached && !fastForwardUnread && !fastForwardShortcutHolding;

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
                        _state = State.Auto;
                        autoModeStarts.Invoke();
                        break;
                    case State.FastForward:
                        if (stopFastForward)
                        {
                            int clicks = configManager.GetInt(FastForwardUnreadFirstShownKey);
                            if (clicks < HintFastForwardUnreadClicks)
                            {
                                Alert.Show("dialogue.noreadtext");
                                configManager.SetInt(FastForwardUnreadFirstShownKey, clicks + 1);
                            }
                            else if (clicks == HintFastForwardUnreadClicks)
                            {
                                Alert.Show("dialogue.hint.fastforwardunread");
                                configManager.SetInt(FastForwardUnreadFirstShownKey, clicks + 1);
                            }
                            else
                            {
                                Alert.Show("dialogue.noreadtext");
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

        public bool isDialogueReached { get; private set; }
        public bool fastForwardUnread { get; set; }

        private bool _fastForwardShortcutHolding;

        public bool fastForwardShortcutHolding
        {
            get => _fastForwardShortcutHolding;
            set
            {
                if (_fastForwardShortcutHolding == value)
                {
                    return;
                }

                _fastForwardShortcutHolding = value;
                state = value ? State.FastForward : State.Normal;
            }
        }

        // Update state and isReadDialogue before OnDialogueChanged is invoked
        private void OnDialogueChangedEarly(DialogueChangedData dialogueData)
        {
            isDialogueReached = dialogueData.isReachedAnyHistory;

            if (isFastForward)
            {
                if (stopFastForward)
                {
                    state = State.Normal;
                }
                else
                {
                    NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                }
            }
        }
    }
}
