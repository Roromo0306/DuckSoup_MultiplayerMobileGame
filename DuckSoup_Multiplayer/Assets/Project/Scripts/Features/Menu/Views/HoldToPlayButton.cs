
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuckSoup.Features.Menu.Views
{
    /// <summary>
    /// Botón que requiere mantener pulsado
    /// durante un tiempo determinado.
    /// </summary>
    public sealed class HoldToPlayButton : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
    {
        [Header("Progress")]
        [SerializeField]
        private Image progressFill;

        [SerializeField, Min(0.1f)]
        private float requiredHoldSeconds = 1.2f;

        public event Action Completed;

        private bool isHolding;
        private bool isCompleted;

        private int activePointerId;
        private float elapsed;

        private void Awake()
        {
            if (progressFill != null)
            {
                progressFill.type = Image.Type.Filled;

                progressFill.fillMethod =
                    Image.FillMethod.Horizontal;

                progressFill.fillOrigin = 0;
            }

            ResetHold();
        }

        public void OnPointerDown(
            PointerEventData eventData)
        {
            if (!isActiveAndEnabled ||
                isHolding ||
                isCompleted)
            {
                return;
            }

            if (eventData.button !=
                PointerEventData.InputButton.Left)
            {
                return;
            }

            activePointerId = eventData.pointerId;

            elapsed = 0f;
            isHolding = true;

            UpdateVisual();
        }

        public void OnPointerUp(
            PointerEventData eventData)
        {
            if (!isHolding ||
                eventData.pointerId != activePointerId)
            {
                return;
            }

            ResetHold();
        }

        public void OnPointerExit(
            PointerEventData eventData)
        {
            if (isHolding &&
                eventData.pointerId == activePointerId)
            {
                ResetHold();
            }
        }

        private void Update()
        {
            if (!isHolding || isCompleted)
                return;

            elapsed += Time.unscaledDeltaTime;

            UpdateVisual();

            if (elapsed < requiredHoldSeconds)
                return;

            isHolding = false;
            isCompleted = true;

            elapsed = requiredHoldSeconds;

            UpdateVisual();

            Completed?.Invoke();
        }

        private void UpdateVisual()
        {
            if (progressFill == null)
                return;

            progressFill.fillAmount = Mathf.Clamp01(
                elapsed / requiredHoldSeconds);
        }

        public void ResetHold()
        {
            isHolding = false;
            isCompleted = false;
            elapsed = 0f;

            if (progressFill != null)
            {
                progressFill.fillAmount = 0f;
            }
        }

        private void OnDisable()
        {
            ResetHold();
        }
    }
}
