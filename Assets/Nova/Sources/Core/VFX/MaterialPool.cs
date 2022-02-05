using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class MaterialPool : MonoBehaviour
    {
        // Keep Renderer's default material, used when turning off VFX on the Renderer
        // defaultMaterial is null for CameraController
        private Material _defaultMaterial;

        public Material defaultMaterial
        {
            get => _defaultMaterial;
            set
            {
                if (_defaultMaterial == value)
                {
                    return;
                }

                Utils.DestroyMaterial(_defaultMaterial);
                _defaultMaterial = value;
            }
        }

        private void Awake()
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                defaultMaterial = renderer.material;
            }
        }

        private void OnDestroy()
        {
            defaultMaterial = null;
            factory.Dispose();
        }

        public readonly MaterialFactory factory = new MaterialFactory();

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