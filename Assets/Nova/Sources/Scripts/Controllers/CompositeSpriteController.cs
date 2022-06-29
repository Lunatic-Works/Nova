using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    public class CompositeSpriteController : OverlayTextureChangerBase
    {
        public string imageFolder;
        public string currentImageName { get; protected set; }
        private static Mesh quad;
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        // public 

        protected override void Awake()
        {
            base.Awake();
            if (quad == null)
            {
                quad = new Mesh();
                quad.vertices = new []
                {
                    new Vector3(-1, -1, 0),
                    new Vector3( 1, -1, 0),
                    new Vector3(-1,  1, 0),
                    new Vector3( 1,  1, 0),
                };
                quad.uv = new []
                {
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1),
                };
                quad.triangles = new []
                {
                    0, 2, 1,
                    2, 3, 1
                };
            }
        }
    }
}
