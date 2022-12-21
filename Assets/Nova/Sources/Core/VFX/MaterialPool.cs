using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class MaterialPool : MonoBehaviour
    {
        // Keep Renderer's default material, used when turning off VFX on the Renderer
        // defaultMaterial is null for PostProcessing
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

                // TODO: Should we destroy this when using URP?
                // Utils.DestroyObject(_defaultMaterial);
                _defaultMaterial = value;
            }
        }

        private void Awake()
        {
            if (TryGetComponent<Renderer>(out var renderer))
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

        // Export to Lua
        public static MaterialPool Ensure(GameObject gameObject)
        {
            return gameObject.Ensure<MaterialPool>();
        }
    }
}
