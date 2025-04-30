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

        public MaterialFloatAnimationProperty(Material material, string propertyName, float startValue,
            float targetValue) :
            base(nameof(MaterialFloatAnimationProperty) + ":" + material + ":" + propertyName, startValue, targetValue)
        {
            this.material = material;
            propertyID = Shader.PropertyToID(propertyName);
        }

        public MaterialFloatAnimationProperty(Material material, string propertyName, float targetValue) :
            base(nameof(MaterialFloatAnimationProperty) + ":" + material + ":" + propertyName, targetValue)
        {
            this.material = material;
            propertyID = Shader.PropertyToID(propertyName);
        }

        public MaterialFloatAnimationProperty(Material material, string propertyName, float deltaValue,
            UseRelativeValue useRelativeValue) :
            base(nameof(MaterialFloatAnimationProperty) + ":" + material + ":" + propertyName, deltaValue, useRelativeValue)
        {
            this.material = material;
            propertyID = Shader.PropertyToID(propertyName);
        }
    }
}
