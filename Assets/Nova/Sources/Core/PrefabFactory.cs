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

        // For Debug
        [SerializeField] private int bufferCount;
        [SerializeField] private int historyMaxCount;

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
            if (buffer.Count == maxBufferSize)
            {
                Destroy(go);
                return;
            }

            go.SetActive(false);
            go.transform.SetParent(transform, false);
            buffer.Push(go);
            bufferCount++;

            if (bufferCount > historyMaxCount)
            {
                historyMaxCount = bufferCount;
            }
        }
    }
}
