namespace Nova
{
    [ExportCustomType]
    public class CameraSizeAnimationProperty : FloatBaseAnimationProperty
    {
        private readonly CameraController camera;

        protected override float currentValue
        {
            get => camera.size;
            set => camera.size = value;
        }

        protected override float CombineDelta(float a, float b) => a * b;

        public CameraSizeAnimationProperty(CameraController camera, float startValue, float targetValue) : base(
            startValue, targetValue)
        {
            this.camera = camera;
        }

        public CameraSizeAnimationProperty(CameraController camera, float targetValue) : base(targetValue)
        {
            this.camera = camera;
        }

        public CameraSizeAnimationProperty(CameraController camera, float deltaValue, UseRelativeValue useRelativeValue)
            : base(deltaValue, useRelativeValue)
        {
            this.camera = camera;
        }
    }
}
