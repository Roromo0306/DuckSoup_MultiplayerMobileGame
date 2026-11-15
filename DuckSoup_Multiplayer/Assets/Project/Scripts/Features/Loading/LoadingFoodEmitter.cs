
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.Loading
{
    [DisallowMultipleComponent]
    public sealed class LoadingFoodEmitter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform spawnArea;
        [SerializeField] private RectTransform fxRoot;

        [Header("Food sprites")]
        [SerializeField] private Sprite[] foodSprites;

        [Header("Emission")]
        [SerializeField] private bool playOnEnable = true;

        [SerializeField, Range(0.1f, 20f)]
        private float piecesPerSecond = 5f;

        [SerializeField, Range(1, 40)]
        private int maxActivePieces = 18;

        [Header("Movement")]
        [SerializeField]
        private Vector2 lifetimeRange =
            new Vector2(1.0f, 1.6f);

        [SerializeField]
        private Vector2 riseDistanceRange =
            new Vector2(90f, 155f);

        [SerializeField, Min(0f)]
        private float horizontalDrift = 35f;

        [SerializeField, Min(0f)]
        private float wobbleDistance = 8f;

        [SerializeField, Min(0f)]
        private float rotationSpeed = 110f;

        [Header("Appearance")]
        [SerializeField]
        private Vector2 sizeRange =
            new Vector2(20f, 35f);

        private readonly List<FoodPiece> active =
            new List<FoodPiece>();

        private readonly Stack<FoodPiece> pool =
            new Stack<FoodPiece>();

        private bool emitting;
        private float accumulator;
        private bool warned;

        private sealed class FoodPiece
        {
            public RectTransform rect;
            public Image image;

            public Vector2 startPosition;
            public float age;
            public float lifetime;
            public float riseDistance;
            public float drift;
            public float wobble;
            public float phase;
            public float startRotation;
            public float spinSpeed;
        }

        private void Awake()
        {
            if (fxRoot == null)
                fxRoot = transform as RectTransform;
        }

        private void OnEnable()
        {
            emitting = playOnEnable;
        }

        public void Play()
        {
            emitting = true;
        }

        public void Stop()
        {
            emitting = false;
            accumulator = 0f;

            for (int i = active.Count - 1; i >= 0; i--)
                Recycle(i);
        }

        private void Update()
        {
            if (!ValidateReferences())
                return;

            float dt = Time.unscaledDeltaTime;

            if (emitting)
            {
                accumulator += piecesPerSecond * dt;

                while (accumulator >= 1f &&
                       active.Count < maxActivePieces)
                {
                    accumulator -= 1f;
                    SpawnPiece();
                }

                if (active.Count >= maxActivePieces)
                    accumulator = Mathf.Min(accumulator, 1f);
            }

            for (int i = active.Count - 1; i >= 0; i--)
            {
                FoodPiece p = active[i];

                p.age += dt;

                RenderPiece(p);

                if (p.age >= p.lifetime)
                    Recycle(i);
            }
        }

        private bool ValidateReferences()
        {
            bool valid =
                spawnArea != null &&
                fxRoot != null &&
                foodSprites != null &&
                foodSprites.Length > 0;

            if (!valid && !warned)
            {
                warned = true;
                Debug.LogWarning(
                    "[LoadingFoodEmitter] Assign Spawn Area, " +
                    "FX Root and Food Sprites.",
                    this);
            }

            return valid;
        }

        private FoodPiece CreatePiece()
        {
            GameObject go = new GameObject(
                "FloatingFoodPiece",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            RectTransform rect =
                go.GetComponent<RectTransform>();

            rect.SetParent(fxRoot, false);

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Image image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;

            return new FoodPiece
            {
                rect = rect,
                image = image
            };
        }

        private void SpawnPiece()
        {
            Sprite sprite = foodSprites[
                Random.Range(0, foodSprites.Length)];

            if (sprite == null)
                return;

            FoodPiece p = pool.Count > 0
                ? pool.Pop()
                : CreatePiece();

            p.rect.gameObject.SetActive(true);
            p.rect.SetAsLastSibling();

            p.image.sprite = sprite;
            p.image.color = Color.white;

            // Elegimos un punto dentro de la abertura
            // de la olla y lo convertimos al FX Root.
            Vector3 spawnLocal = new Vector3(
                Random.Range(
                    spawnArea.rect.xMin,
                    spawnArea.rect.xMax),
                Random.Range(
                    spawnArea.rect.yMin,
                    spawnArea.rect.yMax),
                0f);

            Vector3 spawnWorld =
                spawnArea.TransformPoint(spawnLocal);

            Vector3 effectLocal =
                fxRoot.InverseTransformPoint(spawnWorld);

            p.startPosition =
                new Vector2(effectLocal.x, effectLocal.y);

            float size = Between(sizeRange);

            p.rect.sizeDelta =
                new Vector2(size, size);

            p.age = 0f;

            p.lifetime = Mathf.Max(
                0.1f,
                Between(lifetimeRange));

            p.riseDistance =
                Between(riseDistanceRange);

            p.drift =
                Random.Range(
                    -horizontalDrift,
                    horizontalDrift);

            p.wobble =
                Random.Range(
                    0f,
                    wobbleDistance);

            p.phase =
                Random.Range(0f, Mathf.PI * 2f);

            p.startRotation =
                Random.Range(-45f, 45f);

            p.spinSpeed =
                Random.Range(
                    -rotationSpeed,
                    rotationSpeed);

            active.Add(p);
            RenderPiece(p);
        }

        private void RenderPiece(FoodPiece p)
        {
            float t = Mathf.Clamp01(
                p.age / p.lifetime);

            float easedRise =
                1f - Mathf.Pow(1f - t, 1.4f);

            float x =
                p.startPosition.x +
                p.drift * t +
                Mathf.Sin(p.age * 6f + p.phase) *
                p.wobble * t;

            float y =
                p.startPosition.y +
                p.riseDistance * easedRise;

            p.rect.anchoredPosition =
                new Vector2(x, y);

            float angle =
                p.startRotation +
                p.spinSpeed * p.age;

            p.rect.localRotation =
                Quaternion.Euler(0f, 0f, angle);

            // Entrada suave y desaparición.
            float fadeIn =
                Mathf.Clamp01(t / 0.18f);

            float fadeOut =
                Mathf.Clamp01((1f - t) / 0.30f);

            Color color = Color.white;
            color.a = Mathf.Min(fadeIn, fadeOut);
            p.image.color = color;

            float scale =
                Mathf.Lerp(0.75f, 1.05f, t);

            p.rect.localScale =
                Vector3.one * scale;
        }

        private static float Between(Vector2 range)
        {
            return Random.Range(
                Mathf.Min(range.x, range.y),
                Mathf.Max(range.x, range.y));
        }

        private void Recycle(int index)
        {
            FoodPiece p = active[index];
            active.RemoveAt(index);

            if (p.rect != null)
                p.rect.gameObject.SetActive(false);

            pool.Push(p);
        }

        private void OnDisable()
        {
            Stop();
        }
    }
}
