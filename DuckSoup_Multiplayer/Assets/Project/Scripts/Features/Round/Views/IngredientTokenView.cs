
using System;
using DuckSoup.Features.Round.Contracts;
using DuckSoup.Features.Round.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuckSoup.Features.Round.Views
{
    /// <summary>
    /// Vista interactiva de un ingrediente.
    ///
    /// Controla el arrastre visual, pero delega
    /// todas las reglas en RoundBoardView
    /// y su controlador.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class IngredientTokenView : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [SerializeField]
        private TextMeshProUGUI ingredientText;

        private RoundBoardView board;
        private RectTransform dragLayer;
        private RectTransform tokenRect;
        private Image backgroundImage;

        private Transform originalParent;
        private int originalSiblingIndex;

        private GameObject placeholder;

        private int ownerPointerId;
        private bool isHeld;
        private bool isDragging;

        private Vector2 pointerOffset;

        public string TokenId { get; private set; }

        private void Awake()
        {
            tokenRect = GetComponent<RectTransform>();
            backgroundImage = GetComponent<Image>();
        }

        public void Initialize(
            IngredientTokenSnapshot snapshot,
            RoundBoardView board,
            RectTransform dragLayer)
        {
            this.board = board ??
                throw new ArgumentNullException(nameof(board));

            this.dragLayer = dragLayer ??
                throw new ArgumentNullException(nameof(dragLayer));

            Refresh(snapshot);
        }

        /// <summary>
        /// Actualiza únicamente la presentación.
        /// No reinicia el gesto activo.
        /// </summary>
        public void Refresh(
            IngredientTokenSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            TokenId = snapshot.Id;

            backgroundImage.raycastTarget =
                !snapshot.IsConsumed;

            if (snapshot.IsConsumed)
            {
                ingredientText.text = "OK";
                backgroundImage.color =
                    new Color(0.50f, 0.50f, 0.50f);

                return;
            }

            if (snapshot.IsRevealed)
            {
                ingredientText.text =
                    GetIngredientLabel(snapshot.VisibleIngredient);

                backgroundImage.color =
                    new Color(0.95f, 0.72f, 0.35f);
            }
            else
            {
                ingredientText.text = "?";

                backgroundImage.color =
                    new Color(0.60f, 0.37f, 0.19f);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isHeld || board == null)
                return;

            if (eventData.button !=
                PointerEventData.InputButton.Left)
            {
                return;
            }

            if (!board.TryBeginToken(
                TokenId, eventData.pointerId))
            {
                return;
            }

            ownerPointerId = eventData.pointerId;
            isHeld = true;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsOwner(eventData))
                return;

            BeginVisualDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsOwner(eventData) || !isDragging)
                return;

            UpdateDragPosition(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            FinishGesture(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Seguridad por si el módulo de entrada
            // finaliza el drag antes de PointerUp.
            // Solo se procesará una vez.
            FinishGesture(eventData);
        }

        private bool IsOwner(PointerEventData eventData)
        {
            return isHeld &&
                eventData.pointerId == ownerPointerId;
        }

        /// <summary>
        /// Saca temporalmente la ficha del Grid.
        /// Un placeholder conserva su hueco.
        /// </summary>
        private void BeginVisualDrag(
            PointerEventData eventData)
        {
            if (isDragging)
                return;

            originalParent = transform.parent;
            originalSiblingIndex =
                transform.GetSiblingIndex();

            placeholder = new GameObject(
                "TokenPlaceholder",
                typeof(RectTransform));

            placeholder.transform.SetParent(
                originalParent, false);

            placeholder.transform.SetSiblingIndex(
                originalSiblingIndex);

            // El token pasa a una capa libre,
            // sin las restricciones del GridLayoutGroup.
            tokenRect.SetParent(dragLayer, true);
            tokenRect.SetAsLastSibling();

            isDragging = true;

            if (RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    dragLayer,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPointer))
            {
                Vector3 localToken =
                    dragLayer.InverseTransformPoint(
                        tokenRect.position);

                pointerOffset = new Vector2(
                    localToken.x - localPointer.x,
                    localToken.y - localPointer.y);
            }
            else
            {
                pointerOffset = Vector2.zero;
            }

            UpdateDragPosition(eventData);
        }

        private void UpdateDragPosition(
            PointerEventData eventData)
        {
            if (!RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    dragLayer,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPointer))
            {
                return;
            }

            Vector2 targetPosition =
                localPointer + pointerOffset;

            tokenRect.position = dragLayer.TransformPoint(
                new Vector3(
                    targetPosition.x,
                    targetPosition.y,
                    0f));
        }

        /// <summary>
        /// Finaliza el gesto y pregunta a la mesa
        /// si existe una entrega válida.
        /// </summary>
        private void FinishGesture(
            PointerEventData eventData)
        {
            if (!IsOwner(eventData))
                return;

            string tokenId = TokenId;
            int pointerId = ownerPointerId;
            bool wasDragged = isDragging;

            Vector2 screenPosition = eventData.position;
            Camera eventCamera = eventData.pressEventCamera;

            RestoreVisualPosition();

            board.FinishTokenGesture(
                tokenId,
                pointerId,
                screenPosition,
                eventCamera,
                wasDragged);
        }

        /// <summary>
        /// Cancela un gesto sin intentar
        /// entregar el ingrediente.
        /// </summary>
        public void ForceCancel()
        {
            if (!isHeld)
                return;

            string tokenId = TokenId;
            int pointerId = ownerPointerId;

            RestoreVisualPosition();

            if (board != null)
            {
                board.CancelTokenGesture(
                    tokenId, pointerId);
            }
        }

        private void RestoreVisualPosition()
        {
            if (isDragging)
            {
                if (originalParent != null)
                {
                    tokenRect.SetParent(
                        originalParent, false);

                    tokenRect.SetSiblingIndex(
                        originalSiblingIndex);

                    tokenRect.localScale = Vector3.one;
                    tokenRect.localRotation =
                        Quaternion.identity;
                }

                if (placeholder != null)
                {
                    placeholder.SetActive(false);
                    Destroy(placeholder);
                    placeholder = null;
                }
            }

            isHeld = false;
            isDragging = false;
            originalParent = null;
            pointerOffset = Vector2.zero;
        }

        private void OnDisable()
        {
            ForceCancel();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                ForceCancel();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                ForceCancel();
        }

        private static string GetIngredientLabel(
            IngredientType ingredient)
        {
            switch (ingredient)
            {
                case IngredientType.Carrot:
                    return "ZAN";

                case IngredientType.Potato:
                    return "PAT";

                case IngredientType.Onion:
                    return "CEB";

                case IngredientType.Mushroom:
                    return "SET";

                case IngredientType.Tomato:
                    return "TOM";

                case IngredientType.Pumpkin:
                    return "CAL";

                case IngredientType.Corn:
                    return "MAIZ";

                case IngredientType.Leek:
                    return "PUE";

                default:
                    return "?";
            }
        }
    }
}


