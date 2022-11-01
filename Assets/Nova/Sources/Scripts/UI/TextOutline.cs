using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class TextOutline : Shadow
    {
        private readonly List<UIVertex> uiVerticesList = new List<UIVertex>();

        private static readonly Vector2[] Directions =
        {
            new Vector2(0, 1),
            new Vector2(0, -1),
            new Vector2(1, 0),
            new Vector2(-1, 0),
            new Vector2(1, 1),
            new Vector2(1, -1),
            new Vector2(-1, 1),
            new Vector2(-1, -1)
        };

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || effectDistance.sqrMagnitude < float.Epsilon)
                return;
            vh.GetUIVertexStream(uiVerticesList);
            var num = uiVerticesList.Count * 9;
            if (uiVerticesList.Capacity < num)
                uiVerticesList.Capacity = num;
            var count = 0;
            foreach (var dir in Directions)
            {
                var start = count;
                count = uiVerticesList.Count;
                var _effectDistance = effectDistance;
                var dx = _effectDistance.x * dir.x;
                var dy = _effectDistance.y * dir.y;
                ApplyShadowZeroAlloc(uiVerticesList, effectColor, start, count, dx, dy);
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(uiVerticesList);
        }
    }
}
