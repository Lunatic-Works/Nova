// https://github.com/azixMcAze/Unity-SerializableDictionary

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

public abstract class SerializableDictionaryBase
{
    public abstract class Storage { }

    protected class Dictionary<TKey, TValue> : System.Collections.Generic.Dictionary<TKey, TValue>
    {
        public Dictionary() { }
        public Dictionary(IDictionary<TKey, TValue> dict) : base(dict) { }
        public Dictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}

[Serializable]
public abstract class SerializableDictionaryBase<TKey, TValue, TValueStorage> : SerializableDictionaryBase,
    IDictionary<TKey, TValue>, IDictionary, ISerializationCallbackReceiver, IDeserializationCallback, ISerializable
{
    private Dictionary<TKey, TValue> dict;
    [SerializeField] private TKey[] keys;
    [SerializeField] private TValueStorage[] values;

    public SerializableDictionaryBase()
    {
        dict = new Dictionary<TKey, TValue>();
    }

    public SerializableDictionaryBase(IDictionary<TKey, TValue> dict)
    {
        this.dict = new Dictionary<TKey, TValue>(dict);
    }

    protected abstract void SetValue(TValueStorage[] storage, int i, TValue value);
    protected abstract TValue GetValue(TValueStorage[] storage, int i);

    public void CopyFrom(IDictionary<TKey, TValue> dict)
    {
        this.dict.Clear();
        foreach (var kvp in dict)
        {
            this.dict[kvp.Key] = kvp.Value;
        }
    }

    public void OnAfterDeserialize()
    {
        if (keys != null && values != null && keys.Length == values.Length)
        {
            dict.Clear();
            int n = keys.Length;
            for (int i = 0; i < n; ++i)
            {
                dict[keys[i]] = GetValue(values, i);
            }

            keys = null;
            values = null;
        }
    }

    public void OnBeforeSerialize()
    {
        int n = dict.Count;
        keys = new TKey[n];
        values = new TValueStorage[n];

        int i = 0;
        foreach (var kvp in dict)
        {
            keys[i] = kvp.Key;
            SetValue(values, i, kvp.Value);
            ++i;
        }
    }

    #region IDictionary<TKey, TValue>

    public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>)dict).Keys;

    public ICollection<TValue> Values => ((IDictionary<TKey, TValue>)dict).Values;

    public int Count => ((IDictionary<TKey, TValue>)dict).Count;

    public bool IsReadOnly => ((IDictionary<TKey, TValue>)dict).IsReadOnly;

    public TValue this[TKey key]
    {
        get => ((IDictionary<TKey, TValue>)dict)[key];
        set => ((IDictionary<TKey, TValue>)dict)[key] = value;
    }

    public void Add(TKey key, TValue value)
    {
        ((IDictionary<TKey, TValue>)dict).Add(key, value);
    }

    public bool ContainsKey(TKey key)
    {
        return ((IDictionary<TKey, TValue>)dict).ContainsKey(key);
    }

    public bool Remove(TKey key)
    {
        return ((IDictionary<TKey, TValue>)dict).Remove(key);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        return ((IDictionary<TKey, TValue>)dict).TryGetValue(key, out value);
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        ((IDictionary<TKey, TValue>)dict).Add(item);
    }

    public void Clear()
    {
        ((IDictionary<TKey, TValue>)dict).Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return ((IDictionary<TKey, TValue>)dict).Contains(item);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ((IDictionary<TKey, TValue>)dict).CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return ((IDictionary<TKey, TValue>)dict).Remove(item);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return ((IDictionary<TKey, TValue>)dict).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IDictionary<TKey, TValue>)dict).GetEnumerator();
    }

    #endregion

    #region IDictionary

    public bool IsFixedSize => ((IDictionary)dict).IsFixedSize;

    ICollection IDictionary.Keys => ((IDictionary)dict).Keys;

    ICollection IDictionary.Values => ((IDictionary)dict).Values;

    public bool IsSynchronized => ((IDictionary)dict).IsSynchronized;

    public object SyncRoot => ((IDictionary)dict).SyncRoot;

    public object this[object key]
    {
        get => ((IDictionary)dict)[key];
        set => ((IDictionary)dict)[key] = value;
    }

    public void Add(object key, object value)
    {
        ((IDictionary)dict).Add(key, value);
    }

    public bool Contains(object key)
    {
        return ((IDictionary)dict).Contains(key);
    }

    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
        return ((IDictionary)dict).GetEnumerator();
    }

    public void Remove(object key)
    {
        ((IDictionary)dict).Remove(key);
    }

    public void CopyTo(Array array, int index)
    {
        ((IDictionary)dict).CopyTo(array, index);
    }

    #endregion

    #region IDeserializationCallback

    public void OnDeserialization(object sender)
    {
        ((IDeserializationCallback)dict).OnDeserialization(sender);
    }

    #endregion

    #region ISerializable

    protected SerializableDictionaryBase(SerializationInfo info, StreamingContext context)
    {
        dict = new Dictionary<TKey, TValue>(info, context);
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        ((ISerializable)dict).GetObjectData(info, context);
    }

    #endregion
}

public abstract class SerializableDictionary
{
    public abstract class Storage<T> : SerializableDictionaryBase.Storage
    {
        public T data;
    }
}

[Serializable]
public class SerializableDictionary<TKey, TValue> : SerializableDictionaryBase<TKey, TValue, TValue>
{
    public SerializableDictionary() { }
    public SerializableDictionary(IDictionary<TKey, TValue> dict) : base(dict) { }
    protected SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }

    protected override TValue GetValue(TValue[] storage, int i)
    {
        return storage[i];
    }

    protected override void SetValue(TValue[] storage, int i, TValue value)
    {
        storage[i] = value;
    }
}

[Serializable]
public class SerializableDictionary<TKey, TValue, TValueStorage> : SerializableDictionaryBase<TKey, TValue, TValueStorage>
    where TValueStorage : SerializableDictionary.Storage<TValue>, new()
{
    public SerializableDictionary() { }
    public SerializableDictionary(IDictionary<TKey, TValue> dict) : base(dict) { }
    protected SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }

    protected override TValue GetValue(TValueStorage[] storage, int i)
    {
        return storage[i].data;
    }

    protected override void SetValue(TValueStorage[] storage, int i, TValue value)
    {
        storage[i] = new TValueStorage
        {
            data = value
        };
    }
}
