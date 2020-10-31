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

        private InputMapper _inputMapper;

        private InputMapper inputMapper
        {
            get
            {
                if (_inputMapper == null)
                {
                    _inputMapper = Utils.FindNovaGameController().InputMapper;
                }

                return _inputMapper;
            }
        }

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
            inputMappingList.Refresh();
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

        public void RestoreCurrentKeyMapping()
        {
            keyboardData[currentSelectedKey] =
                inputMapper.keyboard.Data[currentSelectedKey].Select(key => new CompoundKey(key)).ToList();
            inputMappingList.Refresh();
        }

        public void RestoreAll()
        {
            RefreshData();
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
            keyboardData[currentSelectedKey] = inputMapper.GetDefaultKeyboardData()[currentSelectedKey];
            MarkDataDirty();
            inputMappingList.Refresh();
        }

        public void StartModifyCompoundKey(InputMappingListEntry entry)
        {
            compoundKeyRecorder.BeginRecording(entry);
        }
    }
}