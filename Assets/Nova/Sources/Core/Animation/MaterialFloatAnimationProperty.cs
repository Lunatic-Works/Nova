using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class MaterialFloatAnimationProperty : FloatBaseAnimationProperty
    {
        private readonly Material material;
        private readonly int propertyID;

        protected override float currentValue
        {
            get => material.GetFloat(propertyID);
            set => material.SetFloat(propertyID, value);
        }

        private static string GetKey(Material material, string propertyName)
        {
            return string.Join(":", new string[] {
                nameof(MaterialFloatAnimationProperty),
                material.name,
                material.GetInstanceID().ToString(),
                propertyName,
            });
        }

        public MaterialFloatAnimationProperty(Material material, string propertyName, float startValue,
            float targetValue) :
            base(GetKey(material, propertyName), startValue, targetValue)
        {
            this.material = material;
            propertyID = Shader.PropertyToID(propertyName);
        }

        public MaterialFloatAnimationProperty(Material material, string propertyName, float targetValue) :
            base(GetKey(material, propertyName), targetValue)
        {
            this.material = material;
            propertyID = Shader.PropertyToID(propertyName);
        }

        public MaterialFloatAnimationProperty(Material material, string propertyName, float deltaValue,
            UseRelativeValue useRelativeValue) :
            base(GetKey(material, propertyName), deltaValue, useRelativeValue)
        {
            this.material = material;
            propertyID = Shader.PropertyToID(propertyName);
        }
    }
}
