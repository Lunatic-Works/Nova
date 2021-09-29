using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class MaterialPool : MonoBehaviour
    {
        // Keep Renderer's default material, used when turning off VFX on the Renderer
        // defaultMaterial is null for CameraController
        // TODO: will it be freed by UnloadUnusedAssets()?
        public Material defaultMaterial;

        private void Awake()
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                defaultMaterial = renderer.material;
            }
        }

        // TODO: should we call factory.Dispose(), or will those materials be freed by UnloadUnusedAssets()?
        private void OnDestroy()
        {
            defaultMaterial = null;
        }

        public MaterialFactory factory { get; } = new MaterialFactory();

        public Material Get(string shaderName)
        {
            return factory.Get(shaderName);
        }

        public RestorableMaterial GetRestorableMaterial(string shaderName)
        {
            return factory.GetRestorableMaterial(shaderName);
        }

        public static MaterialPool Ensure(GameObject gameObject)
        {
            var pool = gameObject.GetComponent<MaterialPool>();
            if (pool == null)
            {
                pool = gameObject.AddComponent<MaterialPool>();
            }

            return pool;
        }
    }
}