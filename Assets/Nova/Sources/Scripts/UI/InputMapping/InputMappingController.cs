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

        private ActionAssetData actionAsset
        {
            get => inputManager.actionAsset;
            set => inputManager.SetActionAsset(value.data);
        }

        /// <summary>
        /// Action asset before modification
        /// </summary>
        private ActionAssetData oldActionAsset;

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

            oldActionAsset = actionAsset.Clone();
        }

        private InputManager _inputManager;

        public InputManager inputManager
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
                catch (Exception e)
                {
                    // When all bindings are erased, action.bindings.Count might be 1,
                    // but accessing action.bindings[0] will throw an exception.
                    Debug.LogException(e);
                    continue;
                }

                yield return data;
            }
        }

        private static void RemoveAllBindings(InputAction action)
        {
            while (action.bindings.Count > 0)
            {
                try
                {
                    action.ChangeBinding(0).Erase();
                }
                catch (Exception e)
                {
                    // When all bindings are erased, action.bindings.Count might be 1,
                    // but accessing action.bindings[0] will throw an exception.
                    Debug.LogException(e);
                    break;
                }
            }
        }

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
            actionAsset = oldActionAsset;
        }

        private void RefreshBindingData()
        {
            bindingData.Clear();
            bindingData.AddRange(
                GenerateBindingData(currentAction).OrderBy(d => d.displayString));
        }

        private InputMappingListEntry RefreshBindingList()
        {
            RefreshBindingData();
            return inputMappingList.Refresh();
        }

        public void Apply()
        {
            inputManager.Save();
            oldActionAsset = actionAsset.Clone();
        }

        public void RestoreAll()
        {
            actionAsset = oldActionAsset;
            RefreshBindingList();
        }

        public void RestoreCurrentKeyMapping()
        {
            var original = oldActionAsset.GetAction(currentSelectedKey);
            RemoveAllBindings(currentAction);
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
            RemoveAllBindings(currentAction);
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
            var duplicates = new List<(InputAction, InputBinding)>();
            foreach (var ak in mappableKeys)
            {
                if (ak == currentSelectedKey || !actionAsset.TryGetAction(ak, out var action))
                {
                    continue;
                }

                if (!actionAsset.TryGetActionGroup(currentSelectedKey, out var group)
                    || !actionAsset.TryGetActionGroup(ak, out var otherGroup))
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