using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    public class PrefabFactory : MonoBehaviour
    {
        public GameObject prefab;
        public Func<GameObject> creator;
        public int maxBufferSize;

        private readonly Stack<GameObject> buffer = new Stack<GameObject>();
        private readonly HashSet<int> bufferIDs = new HashSet<int>();

        // For debug
        [ReadOnly] [SerializeField] private int bufferCount;
        [ReadOnly] [SerializeField] private int historyMaxCount;

        /// <summary>
        /// Get a instantiated prefab
        /// </summary>
        /// <returns>instantiated prefab</returns>
        public GameObject Get()
        {
            if (buffer.Count == 0)
            {
                return prefab ? Instantiate(prefab) : creator();
            }

            var go = buffer.Pop();
            bufferIDs.Remove(go.GetInstanceID());
            bufferCount--;
            go.SetActive(true);
            return go;
        }

        public T Get<T>()
        {
            return Get().GetComponent<T>();
        }

        /// <summary>
        /// Put used prefab back. this method will not check if the game object is already in the buffer
        /// </summary>
        /// <param name="go">the game object to put back</param>
        public void Put(GameObject go)
        {
            var id = go.GetInstanceID();
            if (bufferIDs.Contains(id))
            {
                Debug.LogWarning($"Nova: Put in PrefabFactory twice: {id} {Utils.GetPath(go)}");
                return;
            }

            if (buffer.Count == maxBufferSize)
            {
                Destroy(go);
                return;
            }

            go.SetActive(false);
            go.transform.SetParent(transform, false);
            buffer.Push(go);
            bufferIDs.Add(id);
            bufferCount++;

            if (bufferCount > historyMaxCount)
            {
                historyMaxCount = bufferCount;
            }
        }
    }
}
