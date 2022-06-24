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
            set => inputManager.SetActionAsset(value);
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
            .Where(ak => !ActionAssetData.IsEditorOnly(ak));
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
            for (var i = 0; i < cnt; ++i)
            {
                InputBindingData data;
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

        public void RemoveBinding(InputBindingData data)
        {
            for (var i = data.endIndex - 1; i >= data.startIndex; --i)
            {
                currentAction.ChangeBinding(i).Erase();
            }

            RefreshBindingList();
        }

        public void AddBinding()
        {
            StartModifyBinding(null);
        }

        private void Start()
        {
            Init();
            _currentSelectedKey = mappableKeys.First();
            abstractKeyList.Refresh();
            RefreshBindingList();
            compoundKeyRecorder.Init(this);
        }

        private void OnDisable()
        {
            Apply();
        }

        private void RefreshBindingData()
        {
            bindingData.Clear();
            bindingData.AddRange(GenerateBindingData(currentAction).OrderBy(d => d.ToString()));
        }

        private void RefreshBindingList()
        {
            RefreshBindingData();
            inputMappingList.Refresh();
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
            actionAsset = new ActionAssetData(inputManager.defaultActionAsset.Clone());
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

        // In all abstract keys other than currentSelectedKey that are in any same group as currentSelectedKey,
        // remove any binding that is in currentSelectedKey
        public void ResolveDuplicate()
        {
            RefreshBindingData();

            foreach (var ak in mappableKeys)
            {
                if (ak == currentSelectedKey ||
                    !actionAsset.TryGetActionGroup(currentSelectedKey, out var group) ||
                    !actionAsset.TryGetActionGroup(ak, out var otherGroup) ||
                    (group & otherGroup) == 0 ||
                    !actionAsset.TryGetAction(ak, out var action))
                {
                    continue;
                }

                foreach (var otherData in GenerateBindingData(action))
                {
                    if (!bindingData.Any(data => data.SameButtonAs(otherData)))
                    {
                        continue;
                    }

                    for (var i = otherData.endIndex - 1; i >= otherData.startIndex; --i)
                    {
                        action.ChangeBinding(i).Erase();
                    }
                }
            }

            RefreshBindingList();
        }
    }
}