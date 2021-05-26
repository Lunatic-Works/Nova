using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova
{
    using AbstractKeyboardSerialized = Dictionary<string, List<string>>;
    using AbstractKeyboardSerializedFull = Dictionary<string, Dictionary<string, List<string>>>;
    using AbstractKeyGroups = Dictionary<AbstractKey, AbstractKeyGroup>;
    using AbstractKeyTags = Dictionary<AbstractKey, bool>;

    public class AbstractKeyboard : IAbstractKeyDevice
    {
        private AbstractKeyboardData mapping;

        public AbstractKeyboardData Data
        {
            get => mapping;
            set => mapping = value;
        }

        private bool inited;

        public void Init()
        {
            if (inited)
            {
                return;
            }

            mapping = new AbstractKeyboardData();
            inited = true;
        }

        public bool GetKey(AbstractKey key)
        {
            return mapping.TryGetValue(key, out var compoundKeys) && compoundKeys.Any(k => k.holding);
        }

        public void Load(string json)
        {
            mapping.Clear();
            var data = JsonConvert.DeserializeObject<AbstractKeyboardSerialized>(json);
            foreach (var pair in data)
            {
                if (Enum.TryParse(pair.Key, out AbstractKey ak))
                {
                    mapping[ak] = pair.Value.Select(CompoundKey.FromString).ToList();
                }
            }
        }

        public void LoadFull(string json, AbstractKeyGroups groups, AbstractKeyTags isEditor)
        {
            mapping.Clear();
            var data = JsonConvert.DeserializeObject<AbstractKeyboardSerializedFull>(json);
            foreach (var pair in data)
            {
                if (Enum.TryParse(pair.Key, out AbstractKey ak))
                {
                    mapping[ak] = pair.Value["Keys"].Select(CompoundKey.FromString).ToList();

                    var group = AbstractKeyGroup.None;
                    foreach (var s in pair.Value["Groups"])
                    {
                        if (Enum.TryParse(s, out AbstractKeyGroup _group))
                        {
                            group |= _group;
                        }
                    }

                    groups[ak] = group;

                    isEditor[ak] = pair.Value["Tags"].Contains("Editor");
                }
            }
        }

        public string Json()
        {
            var data = new AbstractKeyboardSerialized();
            foreach (var pair in mapping)
            {
                data[pair.Key.ToString()] = pair.Value.Select(k => k.ToString()).ToList();
            }

            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        public void Update()
        {
            // do nothing
        }
    }
}