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

        public AbstractKeyboardData keyboardData { get; private set; }

        private InputMapper inputMapper;

        // TODO: not used
        private bool modified = false;

        public void MarkDataDirty()
        {
            modified = true;
        }

        public static IEnumerable<AbstractKey> MappableKeys => Enum.GetValues(typeof(AbstractKey)).Cast<AbstractKey>();

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
            MarkDataDirty();
            inputMappingList.Refresh();
        }

        public void AddCompoundKey()
        {
            currentCompoundKeys.Add(new CompoundKey());
            MarkDataDirty();
            var lastEntry = inputMappingList.Refresh();
            StartModifyCompoundKey(lastEntry);
        }

        private void Awake()
        {
            inputMapper = Utils.FindNovaGameController().InputMapper;
        }

        private void Start()
        {
            RefreshData();
            _currentSelectedKey = MappableKeys.First();
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
            modified = false;
        }

        public void Apply()
        {
            inputMapper.keyboard.Data = keyboardData;
            inputMapper.Save();
            modified = false;
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
            MarkDataDirty();
            inputMappingList.Refresh();
        }

        public void ResetCurrentKeyMappingDefault()
        {
            keyboardData[currentSelectedKey] = inputMapper.GetDefaultCompoundKeys(currentSelectedKey);
            ResolveDuplicate();
            MarkDataDirty();
            inputMappingList.Refresh();
        }

        public void StartModifyCompoundKey(InputMappingListEntry entry)
        {
            compoundKeyRecorder.BeginRecording(entry);
        }

        // Remove the compound keys in all abstract keys other than currentSelectedKey
        // if they are in currentSelectedKey
        public void ResolveDuplicate()
        {
            foreach (var ak in keyboardData.Keys.ToList())
            {
                if (ak == currentSelectedKey)
                {
                    continue;
                }

                keyboardData[ak] = keyboardData[ak].Where(key => !currentCompoundKeys.Contains(key)).ToList();
            }
        }
    }
}