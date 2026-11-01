using System.Collections;
using TMPro;
using UnityEngine;

namespace DuckSoup.Features.GameFeel.UI
{
    // Pop de entrada, WINNER flota y gira, LOSER tiene ola continua por letras.
    [DisallowMultipleComponent]
    public sealed class ResultStatusFeel : MonoBehaviour
    {
        public enum Status { Winner, Loser, Draw }

        [SerializeField] private TMP_Text label;
        [SerializeField, Min(0.05f)] private float popSeconds = 0.36f;
        [SerializeField, Min(0f)] private float winnerBobbing = 12f;
        [SerializeField, Range(0f, 25f)] private float winnerRotation = 7f;
        [SerializeField, Min(0f)] private float waveAmplitude = 11f;
        [SerializeField, Min(0f)] private float waveSpeed = 5f;

        private RectTransform rect;
        private Vector3 normalScale;
        private Vector2 normalPosition;
        private Quaternion normalRotation;
        private Coroutine pop;
        private Status status;
        private bool active;
        private bool popped;
        private float phase;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            if (label == null) label = GetComponent<TMP_Text>();
            normalScale = rect.localScale;
            normalPosition = rect.anchoredPosition;
            normalRotation = rect.localRotation;
        }

        public void Play(Status result)
        {
            StopNow();
            status = result;
            active = true;
            popped = false;
            phase = 0f;
            pop = StartCoroutine(Pop());
        }

        private IEnumerator Pop()
        {
            rect.localScale = normalScale * 0.5f;
            float elapsed = 0f;
            while (elapsed < popSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / popSeconds);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float bump = 1f + Mathf.Sin(t * Mathf.PI) * 0.18f;
                rect.localScale = normalScale * Mathf.Lerp(0.5f, bump, eased);
                yield return null;
            }
            rect.localScale = normalScale;
            popped = true;
            pop = null;
        }

        private void LateUpdate()
        {
            if (!active || !popped) return;
            phase += Time.unscaledDeltaTime;
            if (status == Status.Winner)
            {
                rect.anchoredPosition = normalPosition +
                    Vector2.up * (Mathf.Sin(phase * 4f) * winnerBobbing);
                rect.localRotation = normalRotation *
                    Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 3f) * winnerRotation);
            }
            else if (status == Status.Loser && label != null)
            {
                // Ola por vertices; se reinicia el mesh cada frame, evitando acumulacion.
                label.ForceMeshUpdate();
                TMP_TextInfo info = label.textInfo;
                for (int i = 0; i < info.characterCount; i++)
                {
                    TMP_CharacterInfo c = info.characterInfo[i];
                    if (!c.isVisible) continue;
                    float offset = Mathf.Sin(phase * waveSpeed - i * 0.65f) * waveAmplitude;
                    Vector3[] vertices = info.meshInfo[c.materialReferenceIndex].vertices;
                    int k = c.vertexIndex;
                    for (int n = 0; n < 4; n++) vertices[k + n].y += offset;
                }
                label.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            }
        }

        public void StopNow()
        {
            if (pop != null) StopCoroutine(pop);
            pop = null;
            active = false;
            popped = false;
            if (rect != null)
            {
                rect.localScale = normalScale;
                rect.anchoredPosition = normalPosition;
                rect.localRotation = normalRotation;
            }
            if (label != null) label.ForceMeshUpdate();
        }

        private void OnDisable() => StopNow();
    }
}
