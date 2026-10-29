using System;
using System.Collections;
using UnityEngine;

namespace DuckSoup.Features.GameFeel.UI
{
    // Efectos independientes del dominio; la ficha original vuelve a su hueco.
    [DisallowMultipleComponent]
    public sealed class TokenMotionFeel : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float returnSeconds = 0.22f;
        private UIPunchFeel punch;
        private Coroutine returnRoutine;
        private RectTransform rect;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            punch = GetComponent<UIPunchFeel>();
            if (punch == null) punch = gameObject.AddComponent<UIPunchFeel>();
        }

        public void OnRevealed() => punch.Punch();

        public void OnDragStarted() => punch.ResetNow();

        public void ReturnTo(Vector3 worldHome, Action finished)
        {
            CancelReturn();
            if (rect == null || !isActiveAndEnabled)
            {
                finished?.Invoke();
                return;
            }
            returnRoutine = StartCoroutine(MoveHome(worldHome, finished));
        }

        private IEnumerator MoveHome(Vector3 home, Action finished)
        {
            Vector3 from = rect.position;
            float elapsed = 0f;
            while (elapsed < returnSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / returnSeconds);
                t = 1f - (1f - t) * (1f - t) * (1f - t);
                rect.position = Vector3.LerpUnclamped(from, home, t);
                yield return null;
            }
            rect.position = home;
            returnRoutine = null;
            finished?.Invoke();
        }

        public void CancelReturn()
        {
            if (returnRoutine != null) StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        private void OnDisable()
        {
            CancelReturn();
            if (punch != null) punch.ResetNow();
        }
    }
}
