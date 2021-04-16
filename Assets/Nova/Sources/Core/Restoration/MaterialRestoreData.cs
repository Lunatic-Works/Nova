using System;
using System.Collections.Generic;

namespace Nova
{
    [Serializable]
    public class MaterialRestoreData : IRestoreData
    {
        /// <summary>
        /// if this piece of data comes from a restorable material
        /// </summary>
        public bool isRestorableMaterial;

        public string shaderName;

        /// <summary>
        /// stores color info
        /// </summary>
        public readonly Dictionary<string, Vector4Data> colorDatas = new Dictionary<string, Vector4Data>();

        /// <summary>
        /// stores vector info
        /// </summary>
        public readonly Dictionary<string, Vector4Data> vectorDatas = new Dictionary<string, Vector4Data>();

        /// <summary>
        /// stores float and range info
        /// </summary>
        public readonly Dictionary<string, float> floatDatas = new Dictionary<string, float>();

        /// <summary>
        /// binded texture names
        /// </summary>
        public readonly Dictionary<string, string> textureNames = new Dictionary<string, string>();

        /// <summary>
        /// scale and offset of the texture
        /// </summary>
        public readonly Dictionary<string, Vector4Data> textureScaleOffsets = new Dictionary<string, Vector4Data>();
    }
}