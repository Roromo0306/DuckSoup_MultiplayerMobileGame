
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuckSoup.Features.Menu.Views
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class TableSelectDirectInput :
        MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField]
        private TableSelectorView tableSelector;

        [Header("Game Feel")]
        [Tooltip("Tiempo para dejar terminar la animacion del boton.")]
        [SerializeField, Min(0f)]
        private float confirmationDelay = 0.20f;

        private Button button;
        private Coroutine confirmationRoutine;
        private bool isConfirming;

        private void Awake()
        {
            button = GetComponent<Button>();

            // IMPORTANTE:
            // No desactivamos Button.
            // No desactivamos ButtonPressFeel.
            // No modificamos Button.onClick.
            //
            // El sonido y el Game Feel pueden
            // funcionar como antes.
        }

        public void OnPointerClick(
            PointerEventData eventData)
        {
            if (eventData.button !=
                PointerEventData.InputButton.Left)
            {
                return;
            }

            if (isConfirming ||
                button == null ||
                !button.IsActive() ||
                !button.IsInteractable())
            {
                return;
            }

            if (tableSelector == null)
            {
                Debug.LogError(
                    "[TableSelectDirectInput] " +
                    "Assign Table Selector!",
                    this);

                return;
            }

            isConfirming = true;

            // Esperar a que el boton vuelva
            // suavemente a su escala normal.
            confirmationRoutine =
                StartCoroutine(ConfirmAfterAnimation());
        }

        private IEnumerator ConfirmAfterAnimation()
        {
            if (confirmationDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    confirmationDelay);
            }

            // Se mantiene la confirmacion directa
            // que ya nos funciona correctamente.
            tableSelector.ConfirmSelectionFromDirectInput();

            confirmationRoutine = null;
            isConfirming = false;
        }

        private void OnDisable()
        {
            if (confirmationRoutine != null)
            {
                StopCoroutine(confirmationRoutine);
                confirmationRoutine = null;
            }

            isConfirming = false;
        }
    }
}
