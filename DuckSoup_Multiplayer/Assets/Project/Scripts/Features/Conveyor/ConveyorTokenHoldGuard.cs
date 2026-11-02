using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DuckSoup.Features.Conveyor
{
    /// <summary>
    /// Mantiene inmovil el hueco de un ingrediente mientras un dedo lo
    /// sostiene o mientras se reproduce su retorno tras una entrega fallida.
    /// Se agrega automaticamente a cada token SOLO en la mesa conveyor.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConveyorTokenHoldGuard : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IEndDragHandler
    {
        [SerializeField, Min(0f)]
        private float returnSafetySeconds = 0.4f;

        private ConveyorBeltTable belt;
        private int slotIndex = -1;
        private int pointerId;
        private bool holding;
        private Coroutine releaseRoutine;

        public void Initialize(ConveyorBeltTable owner, int index)
        {
            belt = owner;
            slotIndex = index;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (belt == null || holding ||
                eventData.button != PointerEventData.InputButton.Left) return;

            if (releaseRoutine != null)
            {
                StopCoroutine(releaseRoutine);
                releaseRoutine = null;
            }
            holding = true;
            pointerId = eventData.pointerId;
            belt.SetFrozen(slotIndex, true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ScheduleRelease(eventData.pointerId);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ScheduleRelease(eventData.pointerId);
        }

        private void ScheduleRelease(int id)
        {
            if (!holding || id != pointerId) return;
            holding = false;
            if (releaseRoutine != null) StopCoroutine(releaseRoutine);
            releaseRoutine = StartCoroutine(ReleaseAfterReturn());
        }

        private IEnumerator ReleaseAfterReturn()
        {
            if (returnSafetySeconds > 0f)
                yield return new WaitForSecondsRealtime(returnSafetySeconds);
            if (belt != null) belt.SetFrozen(slotIndex, false);
            releaseRoutine = null;
        }

        private void OnDisable()
        {
            if (releaseRoutine != null) StopCoroutine(releaseRoutine);
            releaseRoutine = null;
            holding = false;
            if (belt != null) belt.SetFrozen(slotIndex, false);
        }
    }
}
