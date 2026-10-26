
using System;
using DuckSoup.Features.Round.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuckSoup.Features.SinglePlayer.Views
{
    [RequireComponent(typeof(Image))]
    public sealed class SinglePlayerTokenView : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [SerializeField]
        private TextMeshProUGUI ingredientText;

        private SinglePlayerBoardView board;
        private RectTransform dragLayer;
        private RectTransform tokenRect;
        private Image backgroundImage;

        private Transform originalParent;
        private int originalSiblingIndex;

        private GameObject placeholder;

        private Vector2 pointerOffset;
        private int ownerPointerId;

        private bool isHeld;
        private bool isDragging;

        public string TokenId { get; private set; }

        private void Awake()
        {
            tokenRect = GetComponent<RectTransform>();
            backgroundImage = GetComponent<Image>();
        }

        public void Initialize(
            SinglePlayerBoardView boardView,
            RectTransform layer)
        {
            board = boardView ??
                throw new ArgumentNullException(
                    nameof(boardView));

            dragLayer = layer ??
                throw new ArgumentNullException(
                    nameof(layer));

            if (ingredientText == null)
                throw new InvalidOperationException(
                    "Falta Ingredient Text.");
        }

        public void Refresh(IngredientTokenSnapshot snapshot)
        {
            if (snapshot == null)
            {
                // Espacio vacío: conservamos
                // la celda del GridLayoutGroup.
                TokenId = null;
                backgroundImage.enabled = false;
                backgroundImage.raycastTarget = false;
                ingredientText.text = "";
                return;
            }

            TokenId = snapshot.Id;

            backgroundImage.enabled = true;
            backgroundImage.raycastTarget =
                !snapshot.IsConsumed;

            if (snapshot.IsRevealed)
            {
                ingredientText.text =
                    GetIngredientLabel(
                        snapshot.VisibleIngredient);

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
            if (isHeld ||
                board == null ||
                string.IsNullOrEmpty(TokenId))
            {
                return;
            }

            if (eventData.button !=
                PointerEventData.InputButton.Left)
                return;

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

        public void OnEndDrag(PointerEventData eventData)
        {
            FinishGesture(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            FinishGesture(eventData);
        }

        private bool IsOwner(PointerEventData eventData)
        {
            return isHeld &&
                eventData.pointerId == ownerPointerId;
        }

        private void BeginVisualDrag(PointerEventData eventData)
        {
            if (isDragging)
                return;

            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();

            // Mantenemos el hueco original del Grid.
            placeholder = new GameObject(
                "TokenPlaceholder",
                typeof(RectTransform));

            placeholder.transform.SetParent(
                originalParent, false);

            placeholder.transform.SetSiblingIndex(
                originalSiblingIndex);

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
                Vector3 tokenLocal =
                    dragLayer.InverseTransformPoint(
                        tokenRect.position);

                pointerOffset = new Vector2(
                    tokenLocal.x - localPointer.x,
                    tokenLocal.y - localPointer.y);
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

            Vector2 position =
                localPointer + pointerOffset;

            tokenRect.position = dragLayer.TransformPoint(
                new Vector3(position.x, position.y, 0f));
        }

        private void FinishGesture(PointerEventData eventData)
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
            DuckSoup.Features.Round.Models.IngredientType type)
        {
            switch (type)
            {
                case DuckSoup.Features.Round.Models.IngredientType.Carrot:
                    return "ZAN";
                case DuckSoup.Features.Round.Models.IngredientType.Potato:
                    return "PAT";
                case DuckSoup.Features.Round.Models.IngredientType.Onion:
                    return "CEB";
                case DuckSoup.Features.Round.Models.IngredientType.Mushroom:
                    return "SET";
                case DuckSoup.Features.Round.Models.IngredientType.Tomato:
                    return "TOM";
                case DuckSoup.Features.Round.Models.IngredientType.Pumpkin:
                    return "CAL";
                case DuckSoup.Features.Round.Models.IngredientType.Corn:
                    return "MAIZ";
                case DuckSoup.Features.Round.Models.IngredientType.Leek:
                    return "PUE";
                default:
                    return "?";
            }
        }
    }
}
