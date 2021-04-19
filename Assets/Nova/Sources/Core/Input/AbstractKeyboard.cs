using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Nova
{
    using AbstractKeyboardSerialized = Dictionary<AbstractKey, List<string>>;

    public class AbstractKeyboard : IAbstractKeyDevice
    {
        private AbstractKeyboardData mapping =
            new AbstractKeyboardData();

        public AbstractKeyboardData Data
        {
            get => mapping;
            set => mapping = value;
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
                mapping[pair.Key] = pair.Value.Select(CompoundKey.FromString).ToList();
            }
        }

        public string Json()
        {
            var data = new AbstractKeyboardSerialized();
            foreach (var pair in mapping)
            {
                data[pair.Key] = pair.Value.Select(k => k.ToString()).ToList();
            }

            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        public void Update()
        {
            // do nothing
        }
    }
}