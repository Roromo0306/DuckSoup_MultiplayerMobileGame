using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.GameFeel.UI
{
    // Emisor de particulas UI: visible tambien con Screen Space - Overlay.
    // Simula dos chorros de confeti lanzados desde izquierda y derecha.
    public sealed class UIConfettiEmitter : MonoBehaviour
    {
        [SerializeField] private RectTransform area;
        [SerializeField, Range(10, 140)] private int particlesPerSide = 42;
        [SerializeField, Min(0.1f)] private float burstDuration = 1.1f;
        [SerializeField, Min(0.1f)] private float particleLifetime = 3f;
        [SerializeField] private Color[] colors =
        {
            new Color(1f, 0.5f, 0.2f), new Color(0.2f, 0.75f, 1f),
            new Color(1f, 0.85f, 0.2f), new Color(0.9f, 0.4f, 0.75f)
        };

        private sealed class Particle
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;
            public float AngularSpeed;
        }

        private readonly List<Particle> particles = new List<Particle>();
        private float emitRemaining;
        private float emitAccumulator;
        private bool playing;

        private void Awake()
        {
            if (area == null) area = GetComponent<RectTransform>();
        }

        public void Burst()
        {
            StopAndClear();
            playing = true;
            emitRemaining = burstDuration;
            emitAccumulator = 0f;
        }

        private void Update()
        {
            if (!playing) return;
            float dt = Time.unscaledDeltaTime;
            if (emitRemaining > 0f)
            {
                emitRemaining -= dt;
                emitAccumulator += dt * (particlesPerSide / burstDuration);
                while (emitAccumulator >= 1f)
                {
                    emitAccumulator -= 1f;
                    Spawn(true);
                    Spawn(false);
                }
            }
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle p = particles[i];
                p.Life -= dt;
                if (p.Life <= 0f)
                {
                    if (p.Rect != null) Destroy(p.Rect.gameObject);
                    particles.RemoveAt(i);
                    continue;
                }
                p.Velocity.y -= 390f * dt;
                p.Position += p.Velocity * dt;
                p.Rect.anchoredPosition = p.Position;
                p.Rect.Rotate(0f, 0f, p.AngularSpeed * dt);
                Color c = p.Image.color;
                c.a = Mathf.Clamp01(p.Life / 0.45f);
                p.Image.color = c;
            }
            if (emitRemaining <= 0f && particles.Count == 0) playing = false;
        }

        private void Spawn(bool left)
        {
            if (area == null) return;
            var go = new GameObject("Confetti", typeof(RectTransform), typeof(Image));
            RectTransform r = go.GetComponent<RectTransform>();
            r.SetParent(area, false);
            r.sizeDelta = new Vector2(Random.Range(9f, 18f), Random.Range(15f, 31f));
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            Image image = go.GetComponent<Image>();
            image.raycastTarget = false;
            if (colors != null && colors.Length > 0)
                image.color = colors[Random.Range(0, colors.Length)];
            Vector2 pos = new Vector2(
                (left ? -0.5f : 0.5f) * area.rect.width,
                Random.Range(-0.25f, 0.1f) * area.rect.height);
            particles.Add(new Particle
            {
                Rect = r, Image = image, Position = pos,
                Velocity = new Vector2(
                    Random.Range(160f, 340f) * (left ? 1f : -1f),
                    Random.Range(180f, 480f)),
                AngularSpeed = Random.Range(-270f, 270f),
                Life = particleLifetime * Random.Range(0.7f, 1.25f)
            });
            r.anchoredPosition = pos;
        }

        public void StopAndClear()
        {
            for (int i = 0; i < particles.Count; i++)
                if (particles[i].Rect != null) Destroy(particles[i].Rect.gameObject);
            particles.Clear();
            emitRemaining = 0f;
            emitAccumulator = 0f;
            playing = false;
        }

        private void OnDisable() => StopAndClear();
    }
}
