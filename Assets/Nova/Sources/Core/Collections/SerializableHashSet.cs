using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Nova
{
    /// <summary>
    /// Implementation of serializable hash set.
    /// The original HashSet is not serializable.
    /// </summary>
    /// <typeparam name="T">Type of values in the hash set</typeparam>
    [Serializable]
    public class SerializableHashSet<T> : HashSet<T>, ISerializedData
    {
        public SerializableHashSet() { }

        protected SerializableHashSet(SerializationInfo info, StreamingContext context)
        {
            foreach (var val in (List<T>)info.GetValue("values", typeof(List<T>)))
            {
                Add(val);
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("values", this.ToList());
        }
    }
}
