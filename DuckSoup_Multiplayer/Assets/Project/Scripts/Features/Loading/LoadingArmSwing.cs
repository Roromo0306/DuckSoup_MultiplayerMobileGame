
using UnityEngine;

namespace DuckSoup.Features.Loading
{
    [DisallowMultipleComponent]
    public sealed class LoadingArmSwing : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField]
        private RectTransform armPivot;

        [Header("Linear Movement")]
        [SerializeField]
        private bool playOnEnable = true;

        [SerializeField, Range(-35f, 35f)]
        private float forwardRotation = 9f;

        [SerializeField, Range(0f, 40f)]
        private float forwardDistance = 5f;

        [SerializeField]
        private Vector2 forwardDirection = Vector2.right;

        [Header("Timing")]
        [Tooltip("Numero de movimientos completos por segundo.")]
        [SerializeField, Range(0.1f, 3f)]
        private float cyclesPerSecond = 0.65f;

        [SerializeField, Range(0f, 360f)]
        private float phaseDegrees = 0f;

        [Header("Debug")]
        [SerializeField]
        private bool testHoldForward = false;

        private Vector2 restPosition;
        private Quaternion restRotation;

        private float elapsed;
        private bool initialized;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            elapsed = 0f;
        }

        private void Initialize()
        {
            if (initialized)
                return;

            if (armPivot == null)
                armPivot = transform as RectTransform;

            if (armPivot == null)
            {
                Debug.LogError(
                    "[LoadingArmSwing] Missing Arm Pivot.",
                    this);
                return;
            }

            restPosition = armPivot.anchoredPosition;
            restRotation = armPivot.localRotation;

            initialized = true;
        }

        private void LateUpdate()
        {
            if (!initialized || armPivot == null)
                return;

            if (!playOnEnable && !testHoldForward)
                return;

            elapsed += Time.unscaledDeltaTime;

            float movement;

            if (testHoldForward)
            {
                movement = 1f;
            }
            else
            {
                // Movimiento triangular:
                // 0 -> 1 -> 0
                //
                // La velocidad es CONSTANTE
                // durante la ida y la vuelta.
                // No se utiliza Sin ni SmoothStep.

                float phase =
                    phaseDegrees / 360f;

                float cycle =
                    Mathf.Repeat(
                        elapsed * cyclesPerSecond + phase,
                        1f);

                movement =
                    1f - Mathf.Abs(2f * cycle - 1f);
            }

            Vector2 direction =
                forwardDirection.sqrMagnitude > 0.001f
                    ? forwardDirection.normalized
                    : Vector2.right;

            // Desplazamiento lineal.
            armPivot.anchoredPosition =
                restPosition +
                direction * forwardDistance * movement;

            // Rotacion lineal desde el hombro.
            armPivot.localRotation =
                restRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    forwardRotation * movement);
        }

        private void OnDisable()
        {
            if (!initialized || armPivot == null)
                return;

            armPivot.anchoredPosition = restPosition;
            armPivot.localRotation = restRotation;

            elapsed = 0f;
        }
    }
}
