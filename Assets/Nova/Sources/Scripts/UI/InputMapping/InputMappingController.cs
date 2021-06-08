using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    public class InputMappingController : MonoBehaviour
    {
        public AbstractKeyList abstractKeyList;
        public InputMappingList inputMappingList;
        public CompoundKeyRecorder compoundKeyRecorder;

        private AbstractKeyboardData keyboardData;

        private InputMapper _inputMapper;

        private InputMapper inputMapper
        {
            get
            {
                if (_inputMapper == null)
                {
                    _inputMapper = Utils.FindNovaGameController().InputMapper;
                    _inputMapper.Init();
                    RefreshData();
                }

                return _inputMapper;
            }
        }

#if UNITY_EDITOR
        public IEnumerable<AbstractKey> mappableKeys => Enum.GetValues(typeof(AbstractKey)).Cast<AbstractKey>();
#else
        public IEnumerable<AbstractKey> mappableKeys => Enum.GetValues(typeof(AbstractKey)).Cast<AbstractKey>()
            .Where(ak => !inputMapper.keyIsEditor[ak]);
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

        public List<CompoundKey> currentCompoundKeys => keyboardData[currentSelectedKey];

        public void DeleteCompoundKey(int index)
        {
            currentCompoundKeys.RemoveAt(index);
            inputMappingList.Refresh();
        }

        public void AddCompoundKey()
        {
            currentCompoundKeys.Add(new CompoundKey());
            var lastEntry = inputMappingList.Refresh();
            StartModifyCompoundKey(lastEntry);
        }

        private void Start()
        {
            RefreshData();
            _currentSelectedKey = mappableKeys.First();
            abstractKeyList.RefreshAll();
            inputMappingList.Refresh();
            compoundKeyRecorder.Init(this);
        }

        private void OnDisable()
        {
            Apply();
        }

        private void RefreshData()
        {
            keyboardData = inputMapper.keyboard.Data.GetCopy();
        }

        public void Apply()
        {
            inputMapper.keyboard.Data = keyboardData;
            inputMapper.Save();
        }

        public void RestoreAll()
        {
            RefreshData();
            inputMappingList.Refresh();
        }

        public void RestoreCurrentKeyMapping()
        {
            keyboardData[currentSelectedKey] =
                inputMapper.keyboard.Data[currentSelectedKey].Select(key => new CompoundKey(key)).ToList();
            ResolveDuplicate();
            inputMappingList.Refresh();
        }

        public void ResetDefault()
        {
            keyboardData = inputMapper.GetDefaultKeyboardData();
            inputMappingList.Refresh();
        }

        public void ResetCurrentKeyMappingDefault()
        {
            keyboardData[currentSelectedKey] = inputMapper.GetDefaultCompoundKeys(currentSelectedKey);
            ResolveDuplicate();
            inputMappingList.Refresh();
        }

        public void StartModifyCompoundKey(InputMappingListEntry entry)
        {
            compoundKeyRecorder.BeginRecording(entry);
        }

        // In all abstract keys other than currentSelectedKey that have any same group as currentSelectedKey,
        // remove any compound key that is in currentSelectedKey
        public void ResolveDuplicate()
        {
            foreach (var ak in keyboardData.Keys)
            {
                if (ak == currentSelectedKey)
                {
                    continue;
                }

                if ((inputMapper.keyGroups[ak] & inputMapper.keyGroups[currentSelectedKey]) == 0)
                {
                    continue;
                }

                keyboardData[ak] = keyboardData[ak].Where(key => !currentCompoundKeys.Contains(key)).ToList();
            }
        }
    }
}