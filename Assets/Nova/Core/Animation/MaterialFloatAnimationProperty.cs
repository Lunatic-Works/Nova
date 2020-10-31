using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class MaterialFloatAnimationProperty : FloatBaseAnimationProperty
    {
        private readonly Material material;
        private readonly int propertyID;

        public MaterialFloatAnimationProperty(Material material, string propertyName, float targetValue) : base(
            targetValue)
        {
            this.material = material;
            propertyID = Shader.PropertyToID(propertyName);
        }

        public MaterialFloatAnimationProperty(Material material, string propertyName, float deltaValue,
            UseRelativeValue useRelativeValue) : base(deltaValue, useRelativeValue)
        {
            this.material = material;
            propertyID = Shader.PropertyToID(propertyName);
        }

        public MaterialFloatAnimationProperty(Material material, string propertyName, float startValue,
            float targetValue) : base(startValue, targetValue)
        {
            this.material = material;
            propertyID = Shader.PropertyToID(propertyName);
        }

        public override string id => "Material Float";

        protected override float currentValue
        {
            get => material.GetFloat(propertyID);
            set => material.SetFloat(propertyID, value);
        }

        protected override float CombineDelta(float a, float b) => a + b;
    }
}