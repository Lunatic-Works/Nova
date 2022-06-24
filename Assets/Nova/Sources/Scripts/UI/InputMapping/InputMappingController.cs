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

        public readonly List<CompositeInputBinding> compositeBindings = new List<CompositeInputBinding>();

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

        private static IEnumerable<CompositeInputBinding> GenerateCompositeBindings(InputAction action)
        {
            var cnt = action.bindings.Count;
            for (var i = 0; i < cnt; ++i)
            {
                CompositeInputBinding compositeBinding;
                try
                {
                    compositeBinding = new CompositeInputBinding(action, i);
                    i = compositeBinding.endIndex - 1;
                }
                catch (Exception e)
                {
                    // TODO: When all bindings are erased, action.bindings.Count might be 1,
                    // but accessing action.bindings[0] will throw an exception.
                    // This may be a bug of Unity.
                    Debug.LogException(e);
                    continue;
                }

                yield return compositeBinding;
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
                    // TODO: When all bindings are erased, action.bindings.Count might be 1,
                    // but accessing action.bindings[0] will throw an exception.
                    // This may be a bug of Unity.
                    Debug.LogException(e);
                    break;
                }
            }
        }

        public void RemoveBinding(CompositeInputBinding data)
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

        private void RefreshCompositeBindings()
        {
            compositeBindings.Clear();
            compositeBindings.AddRange(GenerateCompositeBindings(currentAction).OrderBy(d => d.ToString()));
        }

        private void RefreshBindingList()
        {
            RefreshCompositeBindings();
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
        // remove all bindings that conflict with any binding in currentSelectedKey
        public void ResolveDuplicate()
        {
            var abstractKeyToCompositeBindings = mappableKeys.ToDictionary(key => key,
                key => GenerateCompositeBindings(actionAsset.GetAction(key)).ToList());
            var group = actionAsset.GetActionGroup(currentSelectedKey);
            var compositeBindings = abstractKeyToCompositeBindings[currentSelectedKey];
            var bindingIndicesToRemove = new Dictionary<AbstractKey, List<int>>();
            foreach (var otherAk in mappableKeys)
            {
                if (otherAk == currentSelectedKey || (actionAsset.GetActionGroup(otherAk) & group) == 0)
                {
                    continue;
                }

                var bindingIndices = bindingIndicesToRemove[otherAk] = new List<int>();
                foreach (var otherCb in abstractKeyToCompositeBindings[otherAk])
                {
                    if (!compositeBindings.Any(cb => cb.AnySameBinding(otherCb)))
                    {
                        continue;
                    }

                    for (var i = otherCb.startIndex; i < otherCb.endIndex; ++i)
                    {
                        bindingIndices.Add(i);
                    }
                }
            }

            foreach (var pair in bindingIndicesToRemove)
            {
                var action = actionAsset.GetAction(pair.Key);
                foreach (var i in pair.Value.OrderByDescending(i => i))
                {
                    action.ChangeBinding(i).Erase();
                }
            }

            RefreshBindingList();
        }

        public static void ResolveDuplicateForAction(InputAction action)
        {
            var compositeBindings = InputMappingController.GenerateCompositeBindings(action).ToList();
            // The last composite binding is the newly added one
            var cb = compositeBindings.Last();
            var bindingIndicesToRemove = new List<int>();
            foreach (var otherCb in compositeBindings)
            {
                if (cb == otherCb || !cb.AnySameBinding(otherCb))
                {
                    continue;
                }

                for (var i = otherCb.startIndex; i < otherCb.endIndex; ++i)
                {
                    bindingIndicesToRemove.Add(i);
                }
            }

            foreach (var i in bindingIndicesToRemove.OrderByDescending(i => i))
            {
                action.ChangeBinding(i).Erase();
            }
        }
    }
}