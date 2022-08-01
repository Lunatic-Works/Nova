using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityObject = UnityEngine.Object;

namespace Nova
{
    public class LRUCache<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private class Node
        {
            public TValue Value;
            public readonly LinkedListNode<TKey> QueueNode;

            public Node(TValue value, LinkedListNode<TKey> queueNode)
            {
                Value = value;
                QueueNode = queueNode;
            }
        }

        public readonly int MaxSize;
        public readonly bool AutoDestroy;

        private readonly Dictionary<TKey, Node> dict;
        private readonly LinkedList<TKey> usageQueue;

        public int Count => dict.Count;

        // For inspector
        public int HistoryMaxCount;

        public LRUCache(int maxSize, bool autoDestroy)
        {
            MaxSize = maxSize;
            AutoDestroy = autoDestroy;
            dict = new Dictionary<TKey, Node>(maxSize);
            usageQueue = new LinkedList<TKey>();
        }

        // Will not touch key
        public bool ContainsKey(TKey key)
        {
            return dict.ContainsKey(key);
        }

        private void Touch(Node node)
        {
            usageQueue.Remove(node.QueueNode);
            usageQueue.AddFirst(node.QueueNode);
        }

        public void Touch(TKey key)
        {
            Touch(dict[key]);
        }

        // Will touch key
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (dict.TryGetValue(key, out Node node))
            {
                Touch(node);
                value = node.Value;
                return true;
            }

            value = default;
            return false;
        }

        private void TryDestroyValue(Node node)
        {
            if (!AutoDestroy)
            {
                return;
            }

            if (node.Value is UnityObject obj)
            {
                Utils.DestroyObject(obj);
            }
            else if (node.Value is IDisposable disp)
            {
                disp.Dispose();
            }
        }

        public void Remove(TKey key)
        {
            var node = dict[key]; // Throw if not exist
            TryDestroyValue(node);
            dict.Remove(key);
            usageQueue.Remove(node.QueueNode);
        }

        public void Clear()
        {
            foreach (var node in dict.Values)
            {
                TryDestroyValue(node);
            }

            dict.Clear();
            usageQueue.Clear();
        }

        public IEnumerable<TKey> Keys => usageQueue;

        public IEnumerable<TValue> Values => usageQueue.Select(key => dict[key].Value);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return usageQueue.Select(key => new KeyValuePair<TKey, TValue>(key, dict[key].Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // Both getting and setting will touch key
        public TValue this[TKey key]
        {
            get
            {
                var node = dict[key]; // Throw if not exist
                Touch(node);
                return node.Value;
            }
            set
            {
                if (dict.TryGetValue(key, out var node))
                {
                    node.Value = value;
                }
                else
                {
                    if (dict.Count == MaxSize)
                    {
                        Remove(usageQueue.Last.Value);
                    }

                    dict.Add(key, new Node(value, usageQueue.AddFirst(key)));

                    if (dict.Count > HistoryMaxCount)
                    {
                        HistoryMaxCount = dict.Count;
                    }
                }
            }
        }

        public TValue GetNoTouch(TKey key)
        {
            return dict[key].Value; // Throw if not exist
        }

        public TValue PopLeastUsed()
        {
            TKey key = usageQueue.Last.Value;
            if (key == null)
            {
                throw new InvalidOperationException("Cache is empty");
            }

            usageQueue.RemoveLast();
            TValue value = dict[key].Value;
            dict.Remove(key);
            return value;
        }
    }
}
