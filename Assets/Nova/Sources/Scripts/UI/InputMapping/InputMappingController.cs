using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

namespace Nova
{
    public class InputMappingController : MonoBehaviour
    {
        public AbstractKeyList abstractKeyList;
        public InputMappingList inputMappingList;
        // public CompoundKeyRecorder compoundKeyRecorder;

        // private AbstractKeyboardData keyboardData;
        private ActionAssetData actionAsset;

        private InputSystemManager _inputManager;

        private InputSystemManager inputManager
        {
            get
            {
                if (_inputManager == null)
                {
                    _inputManager = Utils.FindNovaGameController().InputManager;
                    _inputManager.Init();
                    RefreshData();
                }

                return _inputManager;
            }
        }

#if UNITY_EDITOR
        public IEnumerable<AbstractKey> mappableKeys => Enum.GetValues(typeof(AbstractKey)).Cast<AbstractKey>();
#else
        public IEnumerable<AbstractKey> mappableKeys => Enum.GetValues(typeof(AbstractKey)).Cast<AbstractKey>()
            .Where(ak => !inputManager.KeyIsEditor(ak));
#endif

        private AbstractKey _currentSelectedKey;

        public AbstractKey currentSelectedKey
        {
            get => _currentSelectedKey;
            set
            {
                if (_currentSelectedKey == value)
                {
                    return;
                }

                _currentSelectedKey = value;
                abstractKeyList.RefreshSelection();
                inputMappingList.Refresh();
            }
        }

        public InputAction currentAction => actionAsset.GetAction(currentSelectedKey);
        public bool IsRebinding { get; private set; }

        public void DeleteCompoundKey(int index)
        {
            currentAction.ChangeBinding(index).Erase();
            inputMappingList.Refresh();
        }

        public void AddCompoundKey()
        {
            currentAction.AddBinding();
            var lastEntry = inputMappingList.Refresh();
            StartModifyCompoundKey(lastEntry);
        }

        private void Start()
        {
            RefreshData();
            _currentSelectedKey = mappableKeys.First();
            abstractKeyList.RefreshAll();
            inputMappingList.Refresh();
            // compoundKeyRecorder.Init(this);
        }

        private void OnDisable()
        {
            Apply();
        }

        private void RefreshData()
        {
            actionAsset = inputManager.CloneActionAsset();
        }

        public void Apply()
        {
            inputManager.OverrideActionAsset(actionAsset.data);
            inputManager.Save();
        }

        public void RestoreAll()
        {
            RefreshData();
            inputMappingList.Refresh();
        }

        public void RestoreCurrentKeyMapping()
        {
            currentAction.LoadBindingOverridesFromJson(
                inputManager.actionAsset.GetAction(currentSelectedKey).SaveBindingOverridesAsJson());
            ResolveDuplicate();
            inputMappingList.Refresh();
        }

        public void ResetDefault()
        {
            actionAsset.data.RemoveAllBindingOverrides();
            inputMappingList.Refresh();
        }

        public void ResetCurrentKeyMappingDefault()
        {
            currentAction.RemoveAllBindingOverrides();
            ResolveDuplicate();
            inputMappingList.Refresh();
        }

        public void StartModifyCompoundKey(InputMappingListEntry entry)
        {
            IsRebinding = true;
            currentAction.PerformInteractiveRebinding()
                .WithExpectedControlType<ButtonControl>()
                .WithExpectedControlType<KeyControl>()
                .WithTargetBinding(entry.index)
                .Start()
                .OnComplete(operation =>
                {
                    IsRebinding = false;
                    operation.Dispose();
                })
                .OnCancel(operation =>
                {
                    IsRebinding = false;
                    operation.Dispose();
                });
        }

        // In all abstract keys other than currentSelectedKey that have any same group as currentSelectedKey,
        // remove any compound key that is in currentSelectedKey
        public void ResolveDuplicate()
        {
            // TODO: Implement
            /*
            foreach (var ak in keyboardData.Keys.ToList())
            {
                if (ak == currentSelectedKey)
                {
                    continue;
                }

                if ((inputManager.keyGroups[ak] & inputManager.keyGroups[currentSelectedKey]) == 0)
                {
                    continue;
                }

                keyboardData[ak] = keyboardData[ak].Where(key => !currentAction.Contains(key)).ToList();
            }*/
        }
    }
}