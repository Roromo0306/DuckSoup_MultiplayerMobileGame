
using TMPro;
using UnityEngine;

namespace DuckSoup.Features.Loading
{
    [DisallowMultipleComponent]
    public sealed class LoadingTitleBounce : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform titleRoot;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Initial Delay")]
        [Tooltip("Tiempo que permanece quieto el titulo antes de animarse.")]
        [SerializeField, Min(0f)]
        private float initialDelay = 0.8f;

        [Header("Letter Animation")]
        [SerializeField, Min(0.1f)]
        private float jumpDuration = 0.65f;

        [SerializeField, Min(0f)]
        private float jumpHeight = 18f;

        [SerializeField, Range(0f, 0.4f)]
        private float squashAmount = 0.08f;

        [SerializeField, Range(0f, 0.4f)]
        private float stretchAmount = 0.12f;

        [Header("Final Title Pop")]
        [SerializeField, Range(0.1f, 0.4f)]
        private float finalPopFraction = 0.20f;

        [SerializeField, Range(1f, 1.5f)]
        private float finalScale = 1.08f;

        [Header("Testing")]
        [SerializeField, Min(0.5f)]
        private float testDuration = 5f;

        private float totalDuration;
        private float effectiveDelay;
        private float bounceWindow;
        private float actualJumpDuration;
        private float letterDelay;
        private float elapsed;

        private int visibleLetterCount;

        private Vector3 originalScale;

        private bool playing;
        private bool subscribed;

        public bool IsFinished { get; private set; }

        private void Awake()
        {
            FindReferences();

            if (titleRoot != null)
                originalScale = titleRoot.localScale;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void FindReferences()
        {
            if (titleRoot == null)
                titleRoot = transform as RectTransform;

            if (titleText == null)
                titleText =
                    GetComponentInChildren<TextMeshProUGUI>();
        }

        private void Subscribe()
        {
            FindReferences();

            if (titleText == null || subscribed)
                return;

            titleText.OnPreRenderText += AnimateCharacters;
            subscribed = true;
        }

        public void Play(float seconds)
        {
            FindReferences();
            Subscribe();

            if (titleRoot == null || titleText == null)
            {
                Debug.LogError(
                    "[LoadingTitleBounce] Missing references.",
                    this);

                IsFinished = true;
                return;
            }

            titleText.enabled = true;
            titleRoot.localScale = originalScale;

            titleText.ForceMeshUpdate();

            visibleLetterCount = 0;

            TMP_TextInfo info = titleText.textInfo;

            for (int i = 0; i < info.characterCount; i++)
            {
                if (info.characterInfo[i].isVisible)
                    visibleLetterCount++;
            }

            if (visibleLetterCount == 0)
            {
                IsFinished = true;
                return;
            }

            totalDuration = Mathf.Max(0.5f, seconds);

            // Limitar el retraso para que la animacion
            // tenga tiempo suficiente para ejecutarse.
            effectiveDelay = Mathf.Min(
                initialDelay,
                totalDuration * 0.45f);

            // El tiempo activo no incluye el retraso.
            float activeDuration =
                totalDuration - effectiveDelay;

            // Reservar los ultimos segundos
            // para el crecimiento del titulo.
            bounceWindow =
                activeDuration * (1f - finalPopFraction);

            actualJumpDuration = Mathf.Min(
                jumpDuration,
                bounceWindow * 0.8f);

            // Distribuir los saltos entre las letras.
            if (visibleLetterCount > 1)
            {
                letterDelay = Mathf.Max(
                    0f,
                    (bounceWindow - actualJumpDuration) /
                    (visibleLetterCount - 1));
            }
            else
            {
                letterDelay = 0f;
            }

            elapsed = 0f;
            playing = true;
            IsFinished = false;

            titleText.ForceMeshUpdate();

            Debug.Log(
                "[LoadingTitleBounce] Total: " +
                totalDuration.ToString("0.00") +
                "s | Initial delay: " +
                effectiveDelay.ToString("0.00") +
                "s",
                this);
        }

        public void Play()
        {
            Play(testDuration);
        }

        [ContextMenu("TEST - Replay Title")]
        private void TestReplay()
        {
            if (UnityEngine.Application.isPlaying)
                Play(testDuration);
        }

        private void LateUpdate()
        {
            if (!playing || titleText == null)
                return;

            elapsed = Mathf.Min(
                totalDuration,
                elapsed + Time.unscaledDeltaTime);

            // Durante el retraso, el titulo
            // permanece quieto.
            float animationTime =
                elapsed - effectiveDelay;

            titleText.ForceMeshUpdate();

            if (animationTime >= 0f)
            {
                float popDuration = Mathf.Max(
                    0.01f,
                    totalDuration -
                    effectiveDelay -
                    bounceWindow);

                float popProgress = Mathf.Clamp01(
                    (animationTime - bounceWindow) /
                    popDuration);

                float eased =
                    1f - Mathf.Pow(
                        1f - popProgress, 3f);

                float overshoot =
                    Mathf.Sin(popProgress * Mathf.PI) *
                    0.025f;

                float scale =
                    Mathf.Lerp(
                        1f, finalScale, eased) +
                    overshoot;

                titleRoot.localScale =
                    originalScale * scale;
            }
            else
            {
                titleRoot.localScale = originalScale;
            }

            if (elapsed >= totalDuration)
            {
                playing = false;
                IsFinished = true;

                titleRoot.localScale =
                    originalScale * finalScale;
            }
        }

        private void AnimateCharacters(TMP_TextInfo info)
        {
            if (!playing)
                return;

            // Aun no ha terminado el retraso.
            float animationTime =
                elapsed - effectiveDelay;

            if (animationTime < 0f)
                return;

            int letterOrder = 0;

            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo character =
                    info.characterInfo[i];

                if (!character.isVisible)
                    continue;

                float localTime =
                    animationTime -
                    letterOrder * letterDelay;

                letterOrder++;

                if (localTime < 0f ||
                    localTime >= actualJumpDuration)
                {
                    continue;
                }

                float t = Mathf.Clamp01(
                    localTime / actualJumpDuration);

                float wave =
                    Mathf.Sin(t * Mathf.PI);

                // Salto hacia arriba.
                float height =
                    jumpHeight * wave;

                // Stretch durante la subida.
                float scaleX =
                    1f - squashAmount * wave;

                float scaleY =
                    1f + stretchAmount * wave;

                // Squash al aterrizar.
                if (t > 0.7f)
                {
                    float landingT =
                        (t - 0.7f) / 0.3f;

                    float landing =
                        Mathf.Sin(
                            landingT * Mathf.PI);

                    scaleX +=
                        squashAmount * landing;

                    scaleY -=
                        squashAmount * landing;
                }

                int material =
                    character.materialReferenceIndex;

                int vertexIndex =
                    character.vertexIndex;

                Vector3[] vertices =
                    info.meshInfo[material].vertices;

                if (vertexIndex + 3 >= vertices.Length)
                    continue;

                Vector3 center =
                    (vertices[vertexIndex] +
                     vertices[vertexIndex + 2]) * 0.5f;

                for (int v = 0; v < 4; v++)
                {
                    int index = vertexIndex + v;

                    Vector3 offset =
                        vertices[index] - center;

                    offset.x *= scaleX;
                    offset.y *= scaleY;

                    vertices[index] =
                        center +
                        offset +
                        Vector3.up * height;
                }
            }
        }

        private void OnDisable()
        {
            playing = false;

            if (subscribed && titleText != null)
            {
                titleText.OnPreRenderText -=
                    AnimateCharacters;

                subscribed = false;
            }
        }
    }
}
