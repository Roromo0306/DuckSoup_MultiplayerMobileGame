using System;
using System.Collections;
using DuckSoup.Features.GameFeel.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuckSoup.Features.Menu.Views
{
    // Hold to Play especial: confirma DESPUES del retorno a escala normal.
    // El Button.onClick NO se utiliza para iniciar la partida.
    public sealed class HoldToPlayButton : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Image progressFill;
        [SerializeField, Min(0.1f)] private float requiredHoldSeconds = 1.2f;
        public event Action Completed;

        private ButtonPressFeel buttonFeel;
        private bool holding;
        private bool completed;
        private int pointerId;
        private float elapsed;
        private Coroutine confirmRoutine;

        private void Awake()
        {
            if (progressFill != null)
            {
                progressFill.type = Image.Type.Filled;
                progressFill.fillMethod = Image.FillMethod.Horizontal;
                progressFill.fillOrigin = 0;
            }
            buttonFeel = GetComponent<ButtonPressFeel>();
            ResetHold();
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!isActiveAndEnabled || holding || completed ||
                e.button != PointerEventData.InputButton.Left) return;
            pointerId = e.pointerId;
            elapsed = 0f;
            holding = true;
            Render();
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (holding && e.pointerId == pointerId) ResetHold();
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (holding && e.pointerId == pointerId) ResetHold();
        }

        private void Update()
        {
            if (!holding || completed) return;
            elapsed += Time.unscaledDeltaTime;
            Render();
            if (elapsed < requiredHoldSeconds) return;

            holding = false;
            completed = true;
            elapsed = requiredHoldSeconds;
            Render();
            // Si el ButtonFeelInstaller aun no lo instalo en Awake, buscarlo ahora.
            if (buttonFeel == null) buttonFeel = GetComponent<ButtonPressFeel>();
            if (buttonFeel != null)
            {
                buttonFeel.ReleaseVisual();
                confirmRoutine = StartCoroutine(ConfirmAfterReturn());
            }
            else Completed?.Invoke();
        }

        private IEnumerator ConfirmAfterReturn()
        {
            float timeout = 0.5f;
            while (buttonFeel != null && !buttonFeel.IsResting && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
            confirmRoutine = null;
            if (completed) Completed?.Invoke();
        }

        private void Render()
        {
            if (progressFill != null)
                progressFill.fillAmount = Mathf.Clamp01(elapsed / requiredHoldSeconds);
        }

        public void ResetHold()
        {
            if (confirmRoutine != null) StopCoroutine(confirmRoutine);
            confirmRoutine = null;
            holding = false;
            completed = false;
            elapsed = 0f;
            Render();
        }

        private void OnDisable() => ResetHold();
    }
}
