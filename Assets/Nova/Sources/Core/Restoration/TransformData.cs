using System;
using UnityEngine;

namespace Nova
{
    [Serializable]
    public class TransformData
    {
        public readonly Vector3Data localPosition;
        public readonly Vector4Data localRotation;
        public readonly Vector3Data localScale;

        public TransformData(Transform transform)
        {
            localPosition = transform.localPosition;
            localRotation = transform.localRotation;
            localScale = transform.localScale;
        }

        public void Restore(Transform transform)
        {
            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
            transform.localScale = localScale;
        }
    }
}
