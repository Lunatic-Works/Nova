using UnityEngine;

namespace Nova
{
    public class GameOverlayTextureChanger : OverlayTextureChangerBase
    {
        public GameObject actualImageObject;

        private Mesh plane;
        private MeshFilter filter;
        private new MeshRenderer renderer;
        public float pixelsPerUnit = 128f;

        protected override void Awake()
        {
            if (actualImageObject == null)
            {
                actualImageObject = new GameObject();
                actualImageObject.transform.SetParent(transform);
            }
            else
            {
                this.RuntimeAssert(actualImageObject.transform.parent == transform,
                    "actualImageObject must be a child of me");
            }

            plane = new Mesh();
            filter = actualImageObject.AddComponent<MeshFilter>();
            filter.mesh = plane;

            base.Awake();

            renderer = actualImageObject.AddComponent<MeshRenderer>();
            renderer.material = material;
        }

        protected override void ResetSize(float width, float height, Vector2 pivot)
        {
            if (float.IsNaN(width) || float.IsNaN(height))
            {
                plane.Clear();
                return;
            }

            width /= pixelsPerUnit;
            height /= pixelsPerUnit;
            pivot /= pixelsPerUnit;

            var vertices = new[]
            {
                new Vector3(-pivot.x, -pivot.y, 0),
                new Vector3(width - pivot.x, -pivot.y, 0),
                new Vector3(-pivot.x, height - pivot.y, 0),
                new Vector3(width - pivot.x, height - pivot.y, 0)
            };
            plane.vertices = vertices;

            var triangles = new[]
            {
                0, 2, 1,
                2, 3, 1
            };
            plane.triangles = triangles;

            var normals = new[]
            {
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward
            };
            plane.normals = normals;

            var uv = new[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            plane.uv = uv;
        }

        private void OnDestroy()
        {
            Destroy(plane);
        }
    }
}