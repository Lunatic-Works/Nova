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

        private bool inited;

        private void Init()
        {
            if (inited)
            {
                return;
            }

            _inputMapper = Utils.FindNovaGameController().InputMapper;
            _inputMapper.Init();

            inited = true;

            RefreshData();
        }

        private InputMapper _inputMapper;

        private InputMapper inputMapper
        {
            get
            {
                Init();
                return _inputMapper;
            }
        }

#if UNITY_EDITOR
        public IEnumerable<AbstractKey> mappableKeys => Enum.GetValues(typeof(AbstractKey)).Cast<AbstractKey>();
#else
        public IEnumerable<AbstractKey> mappableKeys => Enum.GetValues(typeof(AbstractKey)).Cast<AbstractKey>()
            .Where(ak => !inputMapper.keyIsEditor[ak]);
#endif

        private AbstractKey _currentAbstractKey;

        public AbstractKey currentAbstractKey
        {
            get => _currentAbstractKey;
            set
            {
                if (_currentAbstractKey == value)
                {
                    return;
                }

                _currentAbstractKey = value;
                abstractKeyList.RefreshSelection();
                inputMappingList.Refresh();
            }
        }

        public List<CompoundKey> currentCompoundKeys => keyboardData[currentAbstractKey];

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
            Init();
            _currentAbstractKey = mappableKeys.First();
            abstractKeyList.Refresh();
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
            keyboardData[currentAbstractKey] =
                inputMapper.keyboard.Data[currentAbstractKey].Select(key => new CompoundKey(key)).ToList();
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
            keyboardData[currentAbstractKey] = inputMapper.GetDefaultCompoundKeys(currentAbstractKey);
            ResolveDuplicate();
            inputMappingList.Refresh();
        }

        public void StartModifyCompoundKey(InputMappingEntry entry)
        {
            compoundKeyRecorder.BeginRecording(entry);
        }

        // In all abstract keys other than currentAbstractKey that have any same group as currentAbstractKey,
        // remove any compound key that is in currentAbstractKey
        public void ResolveDuplicate()
        {
            foreach (var ak in keyboardData.Keys.ToList())
            {
                if (ak == currentAbstractKey)
                {
                    continue;
                }

                if ((inputMapper.keyGroups[ak] & inputMapper.keyGroups[currentAbstractKey]) == 0)
                {
                    continue;
                }

                keyboardData[ak] = keyboardData[ak].Where(key => !currentCompoundKeys.Contains(key)).ToList();
            }
        }
    }
}
