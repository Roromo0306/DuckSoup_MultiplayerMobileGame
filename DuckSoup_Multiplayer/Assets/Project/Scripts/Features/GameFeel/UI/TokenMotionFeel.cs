using System;
using System.Collections;
using UnityEngine;

namespace DuckSoup.Features.GameFeel.UI
{
    /// <summary>
    /// Pop al descubrir y retorno suave. Puede seguir un slot que se
    /// esta moviendo (mesa conveyor), o un punto fijo (mesa normal).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TokenMotionFeel : MonoBehaviour
    {
        [SerializeField, Min(0.05f)]
        private float returnSeconds = 0.22f;

        private UIPunchFeel punch;
        private Coroutine returnRoutine;
        private RectTransform rect;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            punch = GetComponent<UIPunchFeel>();
            if (punch == null)
                punch = gameObject.AddComponent<UIPunchFeel>();
        }

        public void OnRevealed()
        {
            punch?.Punch();
        }

        public void OnDragStarted()
        {
            punch?.ResetNow();
        }

        // Se mantiene para el tablero clasico (posicion fija).
        public void ReturnTo(Vector3 worldHome, Action finished)
        {
            StartReturn(() => worldHome, finished);
        }

        // Nuevo: el objetivo cambia de posicion mientras vuelve.
        public void ReturnToMovingSlot(
            RectTransform homeSlot,
            Action finished)
        {
            if (homeSlot == null)
            {
                finished?.Invoke();
                return;
            }

            StartReturn(() =>
                homeSlot != null
                    ? homeSlot.TransformPoint(homeSlot.rect.center)
                    : rect.position,
                finished);
        }

        private void StartReturn(
            Func<Vector3> getTarget,
            Action finished)
        {
            CancelReturn();

            if (rect == null || !isActiveAndEnabled)
            {
                finished?.Invoke();
                return;
            }

            returnRoutine = StartCoroutine(
                AnimateReturn(getTarget, finished));
        }

        private IEnumerator AnimateReturn(
            Func<Vector3> getTarget,
            Action finished)
        {
            Vector3 start = rect.position;
            float elapsed = 0f;

            while (elapsed < returnSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / returnSeconds);
                t = 1f - Mathf.Pow(1f - t, 3f);

                // Se consulta en CADA frame, no al iniciar el drag.
                rect.position = Vector3.LerpUnclamped(
                    start, getTarget(), t);

                yield return null;
            }

            // Al finalizar, aterrizamos exactamente en el slot actual.
            rect.position = getTarget();
            returnRoutine = null;
            finished?.Invoke();
        }

        public void CancelReturn()
        {
            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
                returnRoutine = null;
            }
        }

        private void OnDisable()
        {
            CancelReturn();
            punch?.ResetNow();
        }
    }
}
