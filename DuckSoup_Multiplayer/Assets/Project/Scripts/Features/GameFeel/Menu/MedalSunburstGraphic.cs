using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.GameFeel.Menu
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MedalSunburstGraphic : MaskableGraphic
    {
        [SerializeField, Range(4, 24)]
        private int rayCount = 8;

        [SerializeField, Range(0.1f, 0.9f)]
        private float rayCoverage = 0.55f;

        [SerializeField, Range(0f, 0.6f)]
        private float innerRadius = 0.12f;

        [SerializeField, Range(0f, 1f)]
        private float outerAlpha = 0.1f;

        protected override void Awake()
        {
            base.Awake();

            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform.rect;
            Vector2 center = rect.center;

            float radius =
                Mathf.Min(rect.width, rect.height) * 0.5f;

            int count = Mathf.Clamp(rayCount, 4, 24);

            float step = 360f / count;
            float half = step * Mathf.Clamp01(rayCoverage) * 0.5f;

            Color32 near = color;
            Color32 far = color;

            far.a =
                (byte)(near.a * Mathf.Clamp01(outerAlpha));

            for (int i = 0; i < count; i++)
            {
                Vector2 a = Direction(i * step - half);
                Vector2 b = Direction(i * step + half);

                int first = vh.currentVertCount;

                Add(
                    vh,
                    center + a * (radius * innerRadius),
                    near);

                Add(
                    vh,
                    center + a * radius,
                    far);

                Add(
                    vh,
                    center + b * radius,
                    far);

                Add(
                    vh,
                    center + b * (radius * innerRadius),
                    near);

                vh.AddTriangle(first, first + 1, first + 2);
                vh.AddTriangle(first, first + 2, first + 3);
            }
        }

        private static Vector2 Direction(float degrees)
        {
            float angle = degrees * Mathf.Deg2Rad;

            return new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle));
        }

        private static void Add(
            VertexHelper vh,
            Vector2 point,
            Color32 color)
        {
            vh.AddVert(
                new Vector3(point.x, point.y, 0f),
                color,
                Vector2.zero);
        }
    }
}