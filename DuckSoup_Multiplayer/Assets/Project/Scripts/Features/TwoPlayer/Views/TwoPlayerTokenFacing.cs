
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.TwoPlayer.Views
{
    /// <summary>
    /// Gira visualmente el token 180 grados
    /// sin modificar su RectTransform.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class TwoPlayerTokenFacing : BaseMeshEffect
    {
        private bool rotated;

        public bool IsRotated => rotated;

        public void SetRotated(bool rotate180)
        {
            if (rotated == rotate180)
                return;

            rotated = rotate180;

            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vertices)
        {
            if (!IsActive() ||
                !rotated ||
                vertices == null ||
                vertices.currentVertCount == 0)
            {
                return;
            }

            RectTransform rectTransform =
                transform as RectTransform;

            Vector2 center = rectTransform != null
                ? rectTransform.rect.center
                : Vector2.zero;

            UIVertex vertex = default(UIVertex);

            for (int i = 0;
                 i < vertices.currentVertCount;
                 i++)
            {
                vertices.PopulateUIVertex(ref vertex, i);

                Vector3 position = vertex.position;

                position.x = 2f * center.x - position.x;
                position.y = 2f * center.y - position.y;

                vertex.position = position;

                vertices.SetUIVertex(vertex, i);
            }
        }
    }
}
