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

        public CameraSizeAnimationProperty(CameraController camera, float startValue, float targetValue) :
            base(nameof(CameraSizeAnimationProperty) + ":" + Utils.GetPath(camera), startValue, targetValue)
        {
            this.camera = camera;
        }

        public CameraSizeAnimationProperty(CameraController camera, float targetValue) :
            base(nameof(CameraSizeAnimationProperty) + ":" + Utils.GetPath(camera), targetValue)
        {
            this.camera = camera;
        }

        public CameraSizeAnimationProperty(CameraController camera, float deltaValue, UseRelativeValue useRelativeValue) :
            base(nameof(CameraSizeAnimationProperty) + ":" + Utils.GetPath(camera), deltaValue, useRelativeValue)
        {
            this.camera = camera;
        }
    }
}
