using System;
using System.Collections.Generic;

namespace Nova
{
    [Serializable]
    public class MaterialData
    {
        /// <summary>
        /// if this piece of data comes from a restorable material
        /// </summary>
        public readonly bool isRestorableMaterial;

        public readonly string shaderName;

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

        public MaterialData(bool isRestorableMaterial, string shaderName)
        {
            this.isRestorableMaterial = isRestorableMaterial;
            this.shaderName = shaderName;
        }
    }
}
