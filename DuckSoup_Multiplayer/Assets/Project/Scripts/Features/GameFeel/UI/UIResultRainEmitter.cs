
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.GameFeel.UI
{
    [DisallowMultipleComponent]
    public sealed class UIResultRainEmitter : MonoBehaviour
    {
        private enum EffectType
        {
            Confetti,
            Rain
        }

        [Header("REFERENCES")]
        [Tooltip("Panel completo del jugador.")]
        [SerializeField] private RectTransform area;

        [Tooltip("Capa UI comun, fuera de los paneles girados.")]
        [SerializeField] private RectTransform renderRoot;

        [Header("ORIENTATION")]
        [Tooltip("Invertir manualmente la direccion de caida.")]
        [SerializeField] private bool invertFallDirection = false;

        [Header("EMISSION")]
        [SerializeField] private bool continuousEmission = true;

        [SerializeField, Min(0.1f)]
        private float emissionDuration = 4f;

        [SerializeField, Min(1f)]
        private float confettiPerSecond = 55f;

        [SerializeField, Min(1f)]
        private float rainPerSecond = 120f;

        [SerializeField, Min(0f)]
        private float horizontalPadding = 4f;

        [Header("FALL SPEED")]
        [SerializeField, Min(0.1f)]
        private float confettiFallTime = 2.8f;

        [SerializeField, Min(0.1f)]
        private float rainFallTime = 0.7f;

        [Header("CONFETTI")]
        [SerializeField]
        private Vector2 confettiWidthRange =
            new Vector2(12f, 22f);

        [SerializeField]
        private Vector2 confettiHeightRange =
            new Vector2(9f, 17f);

        [SerializeField, Min(0f)]
        private float confettiSway = 18f;

        [SerializeField]
        private Color[] confettiColors =
        {
            new Color32(255, 212, 90, 255),
            new Color32(255, 110, 100, 255),
            new Color32(95, 190, 245, 255),
            new Color32(115, 213, 145, 255)
        };

        [Header("RAIN")]
        [SerializeField]
        private Vector2 rainWidthRange =
            new Vector2(10f, 16f);

        [SerializeField]
        private Vector2 rainLengthRange =
            new Vector2(50f, 80f);

        [SerializeField]
        private Color rainColor =
            new Color32(120, 200, 255, 255);

        private RectTransform viewport;

        private readonly Vector3[] panelCorners =
            new Vector3[4];

        private readonly List<Piece> active =
            new List<Piece>();

        private readonly Stack<Piece> pool =
            new Stack<Piece>();

        private static Sprite whiteSprite;

        private EffectType currentEffect;
        private bool emitting;
        private bool fallUpwards;

        private float emissionLeft;
        private float spawnAccumulator;

        private sealed class Piece
        {
            public RectTransform rect;
            public Image image;

            public Color color;

            public float age;
            public float lifetime;
            public float startX;
            public float phase;
            public float sway;
            public float angle;
            public float rotationSpeed;
        }

        // --------------------------------------------
        // PUBLIC METHODS
        // --------------------------------------------

        public void PlayWinner()
        {
            StartEffect(EffectType.Confetti);
        }

        public void PlayLoser()
        {
            StartEffect(EffectType.Rain);
        }

        public void StopEffect()
        {
            emitting = false;
            emissionLeft = 0f;
            spawnAccumulator = 0f;

            for (int i = active.Count - 1; i >= 0; i--)
            {
                Recycle(i);
            }
        }

        [ContextMenu("TEST - Winner Confetti")]
        private void TestWinner()
        {
            if (UnityEngine.Application.isPlaying)
                PlayWinner();
        }

        [ContextMenu("TEST - Loser Rain")]
        private void TestLoser()
        {
            if (UnityEngine.Application.isPlaying)
                PlayLoser();
        }

        [ContextMenu("STOP - Effects")]
        private void TestStop()
        {
            StopEffect();
        }

        // --------------------------------------------
        // INITIALIZATION
        // --------------------------------------------

        private void StartEffect(EffectType effect)
        {
            if (!isActiveAndEnabled)
            {
                Debug.LogWarning(
                    "[UIResultRainEmitter] El objeto esta desactivado.",
                    this);
                return;
            }

            if (area == null || renderRoot == null)
            {
                Debug.LogError(
                    "[UIResultRainEmitter] Asigna Area y Render Root.",
                    this);
                return;
            }

            StopEffect();

            EnsureViewport();

            Canvas.ForceUpdateCanvases();
            RefreshViewport();

            if (viewport.rect.width < 10f ||
                viewport.rect.height < 10f)
            {
                Debug.LogError(
                    "[UIResultRainEmitter] El panel no tiene " +
                    "un tamano valido: " + viewport.rect.size,
                    this);
                return;
            }

            // Detectar la orientacion del panel.
            // Un panel girado 180 grados cae hacia
            // su parte inferior visual (arriba en pantalla).
            fallUpwards =
                Vector3.Dot(
                    area.up.normalized,
                    renderRoot.up.normalized) < 0f;

            if (invertFallDirection)
                fallUpwards = !fallUpwards;

            currentEffect = effect;

            emitting = true;
            emissionLeft = emissionDuration;
            spawnAccumulator = 0f;

            viewport.SetAsLastSibling();

            // Primera tanda distribuida por el panel.
            int initial = effect == EffectType.Rain
                ? 24
                : 12;

            for (int i = 0; i < initial; i++)
            {
                SpawnPiece(true);
            }
        }

        private void EnsureViewport()
        {
            if (viewport != null)
                return;

            // IMPORTANTE:
            // Este objeto NO se crea dentro del panel
            // girado, sino dentro del Render Root comun.
            GameObject go = new GameObject(
                "ResultFX_" + gameObject.name,
                typeof(RectTransform),
                typeof(RectMask2D));

            viewport = go.GetComponent<RectTransform>();

            viewport.SetParent(renderRoot, false);

            viewport.anchorMin = new Vector2(0.5f, 0.5f);
            viewport.anchorMax = new Vector2(0.5f, 0.5f);

            viewport.pivot = new Vector2(0.5f, 0.5f);
            viewport.localScale = Vector3.one;
            viewport.localRotation = Quaternion.identity;
        }

        private void RefreshViewport()
        {
            if (viewport == null || area == null ||
                renderRoot == null)
                return;

            area.GetWorldCorners(panelCorners);

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            for (int i = 0; i < 4; i++)
            {
                // Convertimos el panel, aunque este girado,
                // a coordenadas de la capa comun.
                Vector3 local =
                    renderRoot.InverseTransformPoint(
                        panelCorners[i]);

                minX = Mathf.Min(minX, local.x);
                maxX = Mathf.Max(maxX, local.x);

                minY = Mathf.Min(minY, local.y);
                maxY = Mathf.Max(maxY, local.y);
            }

            viewport.sizeDelta = new Vector2(
                Mathf.Max(1f, maxX - minX),
                Mathf.Max(1f, maxY - minY));

            viewport.localPosition = new Vector3(
                (minX + maxX) * 0.5f,
                (minY + maxY) * 0.5f,
                0f);

            viewport.localRotation = Quaternion.identity;
            viewport.localScale = Vector3.one;
        }

        // --------------------------------------------
        // WHITE UI SPRITE
        // --------------------------------------------

        private static Sprite GetWhiteSprite()
        {
            if (whiteSprite != null)
                return whiteSprite;

            Texture2D texture = new Texture2D(
                2, 2, TextureFormat.RGBA32, false);

            texture.SetPixels(new Color[]
            {
                Color.white,
                Color.white,
                Color.white,
                Color.white
            });

            texture.Apply();

            whiteSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f),
                100f);

            return whiteSprite;
        }

        private Piece GetPiece()
        {
            if (pool.Count > 0)
                return pool.Pop();

            GameObject go = new GameObject(
                "FallingPiece",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            RectTransform rect =
                go.GetComponent<RectTransform>();

            rect.SetParent(viewport, false);

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Image image = go.GetComponent<Image>();

            image.sprite = GetWhiteSprite();
            image.raycastTarget = false;
            image.maskable = true;

            return new Piece
            {
                rect = rect,
                image = image
            };
        }

        // --------------------------------------------
        // PARTICLE SPAWNING
        // --------------------------------------------

        private void SpawnPiece(bool randomizeProgress)
        {
            Piece p = GetPiece();

            p.rect.gameObject.SetActive(true);
            p.rect.SetAsLastSibling();

            p.age = 0f;
            p.phase = Random.Range(0f, Mathf.PI * 2f);

            if (currentEffect == EffectType.Rain)
            {
                p.lifetime = rainFallTime *
                    Random.Range(0.9f, 1.1f);

                p.rect.sizeDelta = new Vector2(
                    Between(rainWidthRange),
                    Between(rainLengthRange));

                p.color = rainColor;
                p.sway = 0f;
                p.angle = 0f;
                p.rotationSpeed = 0f;
            }
            else
            {
                p.lifetime = confettiFallTime *
                    Random.Range(0.85f, 1.15f);

                p.rect.sizeDelta = new Vector2(
                    Between(confettiWidthRange),
                    Between(confettiHeightRange));

                p.color =
                    confettiColors != null &&
                    confettiColors.Length > 0
                        ? confettiColors[
                            Random.Range(
                                0, confettiColors.Length)]
                        : Color.yellow;

                p.sway = Random.Range(0f, confettiSway);
                p.angle = Random.Range(0f, 360f);
                p.rotationSpeed =
                    Random.Range(-150f, 150f);
            }

            float halfWidth = Mathf.Max(
                0f,
                viewport.rect.width * 0.5f -
                horizontalPadding -
                p.rect.sizeDelta.x * 0.5f);

            p.startX = Random.Range(
                -halfWidth, halfWidth);

            if (randomizeProgress)
            {
                p.age = p.lifetime *
                    Random.Range(0f, 0.75f);
            }

            p.image.sprite = GetWhiteSprite();
            p.image.enabled = true;
            p.rect.localScale = Vector3.one;

            active.Add(p);
            RenderPiece(p);
        }

        private static float Between(Vector2 range)
        {
            return Random.Range(
                Mathf.Max(1f, Mathf.Min(range.x, range.y)),
                Mathf.Max(1f, Mathf.Max(range.x, range.y)));
        }

        // --------------------------------------------
        // UPDATE
        // --------------------------------------------

        private void Update()
        {
            if (!emitting && active.Count == 0)
                return;

            float dt = Time.unscaledDeltaTime;

            if (emitting)
            {
                float rate = currentEffect == EffectType.Rain
                    ? rainPerSecond
                    : confettiPerSecond;

                spawnAccumulator += dt * rate;

                int amount = Mathf.Min(
                    40,
                    Mathf.FloorToInt(spawnAccumulator));

                spawnAccumulator -= amount;

                for (int i = 0; i < amount; i++)
                    SpawnPiece(false);

                if (!continuousEmission)
                {
                    emissionLeft -= dt;

                    if (emissionLeft <= 0f)
                        emitting = false;
                }
            }

            for (int i = active.Count - 1; i >= 0; i--)
            {
                Piece p = active[i];

                p.age += dt;
                RenderPiece(p);

                if (p.age >= p.lifetime)
                    Recycle(i);
            }
        }

        private void RenderPiece(Piece p)
        {
            float t = Mathf.Clamp01(
                p.age / Mathf.Max(0.1f, p.lifetime));

            float halfHeight =
                viewport.rect.height * 0.5f;

            float startY;
            float endY;

            if (fallUpwards)
            {
                startY = -halfHeight;
                endY = halfHeight +
                    p.rect.sizeDelta.y;
            }
            else
            {
                startY = halfHeight;
                endY = -halfHeight -
                    p.rect.sizeDelta.y;
            }

            float y = Mathf.Lerp(startY, endY, t);

            float x = p.startX +
                p.sway * Mathf.Sin(
                    p.age * 3.5f + p.phase);

            p.rect.anchoredPosition =
                new Vector2(x, y);

            p.rect.localRotation =
                Quaternion.Euler(
                    0f, 0f,
                    p.angle + p.rotationSpeed * p.age);

            Color color = p.color;

            color.a *= Mathf.Clamp01(
                (1f - t) / 0.12f);

            p.image.color = color;
        }

        private void Recycle(int index)
        {
            Piece p = active[index];

            active.RemoveAt(index);

            if (p.rect != null)
                p.rect.gameObject.SetActive(false);

            pool.Push(p);
        }

        private void LateUpdate()
        {
            if (viewport == null)
                return;

            // El area sigue al panel aunque este se
            // anime o cambie de posicion.
            RefreshViewport();
        }

        private void OnDisable()
        {
            StopEffect();
        }

        private void OnDestroy()
        {
            if (viewport != null)
                Destroy(viewport.gameObject);
        }
    }
}
