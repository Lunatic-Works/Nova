// https://gist.github.com/capnslipp/349c18283f2fea316369

using UnityEngine.UI;

namespace Nova
{
    // A concrete subclass of the Unity UI `Graphic` class that just skips drawing.
    // Useful for providing a raycast target without actually drawing anything.
    public class NonDrawingGraphic : Graphic
    {
        public override void SetMaterialDirty() { }

        public override void SetVerticesDirty() { }

        // Probably not necessary since the chain of calls `Rebuild()`->`UpdateGeometry()`->`DoMeshGeneration()`->`OnPopulateMesh()` won't happen; so here really just as a fail-safe.
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}
