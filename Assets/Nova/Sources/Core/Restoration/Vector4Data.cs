using System;
using UnityEngine;

namespace Nova
{
    [Serializable]
    public class Vector4Data
    {
        private float[] _data = {0.0f, 0.0f, 0.0f, 0.0f};

        public float[] data
        {
            get => _data;
            private set => _data = value;
        }

        public Vector4Data() { }

        public Vector4Data(Vector2 v1, Vector2 v2)
        {
            _data[0] = v1.x;
            _data[1] = v1.y;
            _data[2] = v2.x;
            _data[3] = v2.y;
        }

        public void Split(out Vector2 v1, out Vector2 v2)
        {
            v1 = new Vector2(data[0], data[1]);
            v2 = new Vector2(data[2], data[3]);
        }

        public static implicit operator Vector4(Vector4Data data)
        {
            var d = data.data;
            return new Vector4(d[0], d[1], d[2], d[3]);
        }

        public static implicit operator Vector4Data(Vector4 vec)
        {
            return new Vector4Data
            {
                data = new[] {vec.x, vec.y, vec.z, vec.w}
            };
        }

        public static implicit operator Color(Vector4Data data)
        {
            var d = data.data;
            return new Color(d[0], d[1], d[2], d[3]);
        }

        public static implicit operator Vector4Data(Color color)
        {
            return new Vector4Data
            {
                data = new[] {color.r, color.g, color.b, color.a}
            };
        }

        public static implicit operator Quaternion(Vector4Data data)
        {
            var d = data.data;
            return new Quaternion(d[0], d[1], d[2], d[3]);
        }

        public static implicit operator Vector4Data(Quaternion vec)
        {
            return new Vector4Data
            {
                data = new[] {vec.x, vec.y, vec.z, vec.w}
            };
        }
    }
}
