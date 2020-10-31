using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class CellularAutomataController : MonoBehaviour
    {
        private static readonly int DissID = Shader.PropertyToID("_Diss");
        private static readonly int StrengthID = Shader.PropertyToID("_Strength");

        public Material material;
        public Material materialInit;
        public RenderTexture renderTexture;
        public Texture textureInit;
        public Texture textureBlink;
        public float timeStep = 1.0f;

        private RenderTexture buffer;
        private float timeElapsed;
        private bool blinkScheduled;

        private void Start()
        {
            Graphics.Blit(textureInit, renderTexture, materialInit);
            buffer = new RenderTexture(renderTexture.width, renderTexture.height, renderTexture.depth)
            {
                name = "CellularAutomataRenderTexture"
            };
            LuaRuntime.Instance.BindObject("bb", this, "_G");
        }

        private void Update()
        {
            timeElapsed += Time.deltaTime;
            if (timeElapsed > timeStep)
            {
                timeElapsed -= timeStep;

                if (blinkScheduled)
                {
                    blinkScheduled = false;
                    Graphics.Blit(textureBlink, renderTexture, materialInit);
                }

                Graphics.Blit(renderTexture, buffer, material);
                Graphics.Blit(buffer, renderTexture);
            }
        }

        public void Blink(float strength)
        {
            materialInit.SetFloat(StrengthID, strength);
            blinkScheduled = true;
        }

        public void SetDiss(float value)
        {
            material.SetFloat(DissID, value);
        }
    }
}