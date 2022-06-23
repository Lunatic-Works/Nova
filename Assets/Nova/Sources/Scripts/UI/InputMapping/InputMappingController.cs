using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nova
{
    public class InputMappingController : MonoBehaviour
    {
        public AbstractKeyList abstractKeyList;
        public InputMappingList inputMappingList;
        public CompoundKeyRecorder compoundKeyRecorder;

        public readonly List<InputBindingData> bindingData = new List<InputBindingData>();
        private ActionAssetData actionAsset;

        private bool inited;

        private void Init()
        {
            if (inited)
            {
                return;
            }

            _inputManager = Utils.FindNovaGameController().InputManager;
            _inputManager.Init();

            inited = true;

            RefreshData();
        }

        private InputSystemManager _inputManager;

        public InputSystemManager inputManager
        {
            get
            {
                Init();
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
                RefreshBindingList();
            }
        }

        public InputAction currentAction => actionAsset.GetAction(currentSelectedKey);

        public void DeleteCompoundKey(InputBindingData data)
        {
            for (int i = data.endIndex - 1; i >= data.startIndex; i--)
                currentAction.ChangeBinding(i).Erase();
            RefreshBindingList();
        }

        public void AddCompoundKey()
        {
            StartModifyBinding(null);
        }

        private void Start()
        {
            Init();
            _currentSelectedKey = mappableKeys.First();
            abstractKeyList.RefreshAll();
            RefreshBindingList();
            compoundKeyRecorder.Init(this);
        }

        private void OnDisable()
        {
            Apply();
        }

        private void RefreshData()
        {
            actionAsset = inputManager.CloneActionAsset();
        }

        private static IEnumerable<InputBindingData> GenerateBindingData(InputAction action)
        {
            var cnt = action.bindings.Count;
            InputBindingData data;
            for (var i = 0; i < cnt; i++)
            {
                try
                {
                    data = new InputBindingData(action, i);
                    i = data.endIndex - 1;
                }
                catch (InvalidOperationException)
                {
                    // When all bindings are erased, action.bindings.Count might be 1,
                    // but accessing action.bindings[0] will throw an exception.
                    continue;
                }
                yield return data;
            }
        }

        public void RefreshBindingData()
        {
            bindingData.Clear();
            bindingData.AddRange(
                GenerateBindingData(currentAction).OrderBy(d => d.displayString));
        }

        public InputMappingListEntry RefreshBindingList()
        {
            RefreshBindingData();
            return inputMappingList.Refresh();
        }

        public void Apply()
        {
            inputManager.SetActionAsset(actionAsset.data);
            inputManager.Save();
        }

        public void RestoreAll()
        {
            RefreshData();
            RefreshBindingList();
        }

        public void RestoreCurrentKeyMapping()
        {
            var original = inputManager.actionAsset.GetAction(currentSelectedKey);
            while (currentAction.bindings.Count > 0)
            {
                currentAction.ChangeBinding(0).Erase();
            }
            foreach (var binding in original.bindings)
            {
                currentAction.AddBinding(binding);
            }
            ResolveDuplicate();
        }

        public void ResetDefault()
        {
            actionAsset.data.LoadFromJson(inputManager.defaultActionAsset.ToJson());
            RefreshBindingList();
        }

        public void ResetCurrentKeyMappingDefault()
        {
            var original = inputManager.defaultActionAsset.FindAction(currentAction.id);
            while (currentAction.bindings.Count > 0)
            {
                try
                {
                    currentAction.ChangeBinding(0).Erase();
                }
                catch (Exception)
                {
                    // When all bindings are erased, action.bindings.Count might be 1,
                    // but accessing action.bindings[0] will throw an exception.
                    break;
                }
            }
            foreach (var binding in original.bindings)
            {
                currentAction.AddBinding(binding);
            }
            ResolveDuplicate();
        }

        public void StartModifyBinding(InputMappingListEntry entry)
        {
            compoundKeyRecorder.BeginRecording(entry);
        }

        // In all abstract keys other than currentSelectedKey that have any same group as currentSelectedKey,
        // remove any compound key that is in currentSelectedKey
        public void ResolveDuplicate()
        {
            RefreshBindingData();
            List<(InputAction, InputBinding)> duplicates = new List<(InputAction, InputBinding)>();
            foreach (var ak in mappableKeys)
            {
                if (ak == currentSelectedKey || !actionAsset.TryGetAction(ak, out var action))
                {
                    continue;
                }

                if (!actionAsset.TryGetGroup(currentSelectedKey, out var group)
                    || !actionAsset.TryGetGroup(ak, out var otherGroup))
                {
                    continue;
                }

                if (group != otherGroup)
                {
                    continue;
                }

                duplicates.AddRange(GenerateBindingData(action)
                    .Where(d => bindingData.Any(b => b.SameButtonAs(d)))
                    .Select(d => (d.action, d.bindings.First())));
            }
            foreach (var duplicate in duplicates)
            {
                duplicate.Item1.ChangeBinding(duplicate.Item2).Erase();
            }
            RefreshBindingList();
        }
    }
}