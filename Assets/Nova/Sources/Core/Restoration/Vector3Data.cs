using System;
using UnityEngine;

namespace Nova
{
    [Serializable]
    public class Vector3Data
    {
        private float[] _data = {0.0f, 0.0f, 0.0f};

        public float[] data
        {
            get => _data;
            private set => _data = value;
        }

        public static implicit operator Vector3(Vector3Data data)
        {
            var d = data.data;
            return new Vector3(d[0], d[1], d[2]);
        }

        public static implicit operator Vector3Data(Vector3 vec)
        {
            return new Vector3Data
            {
                data = new[] {vec.x, vec.y, vec.z}
            };
        }
    }
}
