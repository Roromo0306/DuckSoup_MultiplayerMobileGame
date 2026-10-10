using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuckSoup.Features.InputTest.Views
{
    [RequireComponent(typeof(Image))]
    public sealed class DraggableTokenProbeView : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("Visual")]
        [SerializeField]
        private string displayName = "Ficha";

        [SerializeField]
        private TextMeshProUGUI statusLabel;

        private RectTransform tokenRect;
        private RectTransform dragLayer;

        private Vector2 initialPosition;
        private Vector2 pointerOffset;

        private int ownerPointerId;

        private bool isHeld;
        private bool isDragging;

        private void Awake()
        {
            tokenRect = GetComponent<RectTransform>();
            dragLayer = transform.parent as RectTransform;

            if (dragLayer == null)
            {
                Debug.LogError("[InputTest] La ficha necesita " +"un RectTransform padre.",this);

                enabled = false;
                return;
            }

            initialPosition = tokenRect.anchoredPosition;

            if (statusLabel == null)
            {
                Debug.LogWarning("[InputTest] Falta el texto de estado.",this);
            }

            RefreshLabel();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isActiveAndEnabled || isHeld)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!TryGetLocalPointerPosition(eventData, out Vector2 localPosition))
                return;

            ownerPointerId = eventData.pointerId;

            isHeld = true;
            isDragging = false;

           
            pointerOffset = tokenRect.anchoredPosition - localPosition;

            RefreshLabel();

            Debug.Log($"[InputTest] {displayName} " +$"reclamada por {ownerPointerId}",this);
        }
      
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsOwner(eventData))
                return;

            isDragging = true;

            UpdateTokenPosition(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsOwner(eventData) || !isDragging)
                return;

            UpdateTokenPosition(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsOwner(eventData))
                return;

            CancelActiveDrag();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsOwner(eventData))
                return;

            CancelActiveDrag();
        }

        public void CancelActiveDrag()
        {
            if (!isHeld)
                return;

            isHeld = false;
            isDragging = false;

            tokenRect.anchoredPosition = initialPosition;

            RefreshLabel();

            Debug.Log($"[InputTest] {displayName} liberada.",this);
        }

        private bool IsOwner(PointerEventData eventData)
        {
            return isHeld && eventData.pointerId == ownerPointerId;
        }

        private void UpdateTokenPosition(PointerEventData eventData)
        {
            if (!TryGetLocalPointerPosition(eventData, out Vector2 localPosition))
                return;

            tokenRect.anchoredPosition = localPosition + pointerOffset;
        }

        private bool TryGetLocalPointerPosition(PointerEventData eventData,out Vector2 localPosition)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer,eventData.position,eventData.pressEventCamera,out localPosition);
        }

        private void RefreshLabel()
        {
            if (statusLabel == null)
                return;

            if (isHeld)
            {
                statusLabel.text = $"{displayName}\n" + $"Puntero: {ownerPointerId}";
            }
            else
            {
                statusLabel.text = $"{displayName}\nLibre";
            }
        }

        private void OnDisable()
        {
            CancelActiveDrag();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                CancelActiveDrag();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                CancelActiveDrag();
        }
    }
}
