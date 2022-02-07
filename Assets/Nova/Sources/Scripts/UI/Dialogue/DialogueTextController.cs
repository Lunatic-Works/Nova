using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nova
{
    public class DialogueTextController : MonoBehaviour
    {
        public GameObject dialogueEntryPrefab;

        private readonly List<DialogueEntryController> _dialogueEntryControllers = new List<DialogueEntryController>();
        public IEnumerable<DialogueEntryController> dialogueEntryControllers => _dialogueEntryControllers;
        public int Count => _dialogueEntryControllers.Count;

        private PrefabFactory _prefabFactory;

        private PrefabFactory prefabFactory
        {
            get
            {
                if (_prefabFactory == null)
                {
                    var t = transform.root.Find("DialogueEntryFactory");
                    GameObject go;
                    if (t == null)
                    {
                        go = new GameObject("DialogueEntryFactory");
                        go.transform.SetParent(transform.root);
                    }
                    else
                    {
                        go = t.gameObject;
                    }

                    var prefabFactoryGO = new GameObject("For " + name);
                    prefabFactoryGO.transform.SetParent(go.transform);
                    _prefabFactory = prefabFactoryGO.AddComponent<PrefabFactory>();
                    _prefabFactory.prefab = dialogueEntryPrefab;
                    _prefabFactory.maxBufferSize = 10;
                }

                return _prefabFactory;
            }
        }

        public void Clear()
        {
            foreach (var dec in _dialogueEntryControllers)
            {
                dec.Clear();
                prefabFactory.Put(dec.gameObject);
            }

            _dialogueEntryControllers.Clear();
        }

        public DialogueEntryController AddEntry(DialogueDisplayData displayData, TextAlignmentOptions alignment,
            Color characterNameColor, Color textColor, string materialName, DialogueEntryLayoutSetting layoutSetting, int textLeftExtraPadding)
        {
            var dec = prefabFactory.Get<DialogueEntryController>();
            var _transform = dec.transform;
            _transform.SetParent(transform);
            _transform.SetAsLastSibling();
            _transform.localPosition = Vector3.zero;
            _transform.localRotation = Quaternion.identity;
            _transform.localScale = Vector3.one;
            _dialogueEntryControllers.Add(dec);
            dec.Init(displayData, alignment, characterNameColor, textColor, materialName,
                layoutSetting, textLeftExtraPadding);
            return dec;
        }
    }
}