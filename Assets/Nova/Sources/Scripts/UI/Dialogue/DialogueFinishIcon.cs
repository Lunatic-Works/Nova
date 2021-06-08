using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [RequireComponent(typeof(RawImage))]
    public class DialogueFinishIcon : MonoBehaviour
    {
        public Camera cubeCamera;
        public GameObject cube;
        public float precessionSpeed = 10.0f;
        public float rotationSpeed = 20.0f;
        public float yAmp = 0.5f;
        public float yFreq = 3.0f;

        private RectTransform rt;
        private RawImage img;
        private RenderTexture renderTarget;
        private Vector3 rotateAxis = Vector3.one;

        private void Awake()
        {
            rt = GetComponent<RectTransform>();
            img = GetComponent<RawImage>();
            this.RuntimeAssert(cube != null && cubeCamera != null, "Missing parameter in DialogueFinishIcon.");
            var rect = rt.rect;
            // Don't do MSAA here. It's hard to check whether the driver supports it.
            cubeCamera.targetTexture = renderTarget = new RenderTexture((int)rect.height, (int)rect.height, 0)
            {
                name = "DialogueFinishIconRenderTexture"
            };
            img.texture = renderTarget;
        }

        private void OnDestroy()
        {
            Destroy(renderTarget);
        }

        private void Update()
        {
            rotateAxis += Random.insideUnitSphere * (precessionSpeed * Time.deltaTime);
            rotateAxis.Normalize();
            cube.transform.Rotate(rotateAxis, rotationSpeed * Time.deltaTime);
            cube.transform.localPosition = Vector3.up * (yAmp * Mathf.Abs(Mathf.Sin(yFreq * Time.time)));
        }
    }
}