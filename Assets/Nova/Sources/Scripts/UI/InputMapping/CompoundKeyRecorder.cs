using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    using KeyStatus = Dictionary<AbstractKey, bool>;

    public class CompoundKeyRecorder : MonoBehaviour, IPointerClickHandler
    {
        public RecordPopupController popupController;

        private InputMapper inputMapper;
        private InputMappingController controller;
        private readonly HashSet<KeyCode> prefixKeys = new HashSet<KeyCode>(CompoundKey.PrefixKeys);
        private KeyStatus keyEnabled;

        private void Awake()
        {
            inputMapper = Utils.FindNovaGameController().InputMapper;
        }

        private void OnEnable()
        {
            keyEnabled = inputMapper.GetEnabledState();
            inputMapper.SetEnableGroup(AbstractKeyGroup.None);
            popupController.entry = entry;
            popupController.Show();
        }

        private void OnDisable()
        {
            popupController.Hide();
            inputMapper.SetEnabledState(keyEnabled);

            if (entry != null)
            {
                entry.FinishModify();

                if (entry.key.isNone)
                {
                    controller.DeleteCompoundKey(entry.index);
                }
                else
                {
                    int duplicatedIndex = -1;
                    for (int i = 0; i < controller.currentCompoundKeys.Count; ++i)
                    {
                        // Assuming there can be at most one duplicated key
                        if (i != entry.index && controller.currentCompoundKeys[i].Equals(entry.key))
                        {
                            duplicatedIndex = i;
                            break;
                        }
                    }

                    if (duplicatedIndex >= 0)
                    {
                        controller.DeleteCompoundKey(entry.index);
                    }
                    else
                    {
                        controller.ResolveDuplicate();
                        entry.RefreshDisplay();
                    }
                }
            }

            entry = null;
        }

        public void Init(InputMappingController controller)
        {
            this.controller = controller;
            gameObject.SetActive(false);
        }

        private bool isPressing = false;
        private InputMappingListEntry entry;

        public void BeginRecording(InputMappingListEntry entry)
        {
            isPressing = false;
            this.entry = entry;
            gameObject.SetActive(true);
        }

        private static bool AnyKeyPressing => CompoundKey.KeyboardKeys.Any(Input.GetKey);

        private static IEnumerable<KeyCode> PressedKey =>
            CompoundKey.KeyboardKeys.Where(Input.GetKey);

        private void WaitPress()
        {
            if (!AnyKeyPressing) return;
            entry.key.Clear();
            isPressing = true;
            HandlePress();
        }

        private void HandlePress()
        {
            if (!AnyKeyPressing)
            {
                gameObject.SetActive(false);
                return;
            }

            var compoundKey = entry.key;
            var dirty = false;

            if (CompoundKey.CtrlIsHolding)
            {
                compoundKey.Ctrl = true;
                dirty = true;
            }

            if (CompoundKey.WinIsHolding)
            {
                compoundKey.Win = true;
                dirty = true;
            }

            if (CompoundKey.AltIsHolding)
            {
                compoundKey.Alt = true;
                dirty = true;
            }

            if (CompoundKey.ShiftIsHolding)
            {
                compoundKey.Shift = true;
                dirty = true;
            }

            foreach (var key in PressedKey)
            {
                if (!prefixKeys.Contains(key))
                {
                    compoundKey.Key = key;
                    dirty = true;
                }
            }

            if (dirty)
            {
                entry.RefreshDisplay();
            }
        }

        private void Update()
        {
            if (entry == null) return;
            if (!isPressing)
            {
                WaitPress();
            }
            else
            {
                HandlePress();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            gameObject.SetActive(false);
        }
    }
}