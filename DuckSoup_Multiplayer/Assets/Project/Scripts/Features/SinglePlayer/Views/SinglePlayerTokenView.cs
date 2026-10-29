using System;
using DuckSoup.Features.GameFeel.UI;
using DuckSoup.Features.Round.Contracts;
using DuckSoup.Features.Round.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuckSoup.Features.SinglePlayer.Views
{
    [RequireComponent(typeof(Image))]
    public sealed class SinglePlayerTokenView : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private TextMeshProUGUI ingredientText;
        private SinglePlayerBoardView board;
        private RectTransform dragLayer;
        private RectTransform tokenRect;
        private Image backgroundImage;
        private TokenMotionFeel motion;
        private Transform originalParent;
        private int originalSiblingIndex;
        private Vector3 originalWorldPosition;
        private GameObject placeholder;
        private Vector2 pointerOffset;
        private int ownerPointerId;
        private bool isHeld;
        private bool isDragging;
        private bool isReturning;

        public string TokenId { get; private set; }

        private void Awake()
        {
            tokenRect = GetComponent<RectTransform>();
            backgroundImage = GetComponent<Image>();
            motion = GetComponent<TokenMotionFeel>();
            if (motion == null) motion = gameObject.AddComponent<TokenMotionFeel>();
        }

        public void Initialize(SinglePlayerBoardView boardView, RectTransform layer)
        {
            board = boardView ?? throw new ArgumentNullException(nameof(boardView));
            dragLayer = layer ?? throw new ArgumentNullException(nameof(layer));
            if (ingredientText == null)
                throw new InvalidOperationException("Asigna IngredientText en SinglePlayerToken.");
        }

        public void Refresh(IngredientTokenSnapshot snapshot)
        {
            if (snapshot == null)
            {
                TokenId = null;
                backgroundImage.enabled = false;
                backgroundImage.raycastTarget = false;
                ingredientText.text = "";
                return;
            }
            TokenId = snapshot.Id;
            backgroundImage.enabled = true;
            backgroundImage.raycastTarget = !snapshot.IsConsumed && !isReturning;
            if (snapshot.IsRevealed)
            {
                ingredientText.text = GetIngredientLabel(snapshot.VisibleIngredient);
                backgroundImage.color = new Color(0.95f, 0.72f, 0.35f);
            }
            else
            {
                ingredientText.text = "?";
                backgroundImage.color = new Color(0.60f, 0.37f, 0.19f);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isHeld || isReturning || board == null ||
                string.IsNullOrEmpty(TokenId) ||
                eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!board.TryBeginToken(TokenId, eventData.pointerId)) return;
            ownerPointerId = eventData.pointerId;
            isHeld = true;
            motion.OnRevealed();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsOwner(eventData) || isDragging) return;
            motion.OnDragStarted();
            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();
            originalWorldPosition = tokenRect.position;

            placeholder = new GameObject("TokenPlaceholder", typeof(RectTransform));
            placeholder.transform.SetParent(originalParent, false);
            placeholder.transform.SetSiblingIndex(originalSiblingIndex);

            tokenRect.SetParent(dragLayer, true);
            tokenRect.SetAsLastSibling();
            isDragging = true;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragLayer, eventData.position, eventData.pressEventCamera, out Vector2 localPointer))
            {
                Vector3 localToken = dragLayer.InverseTransformPoint(tokenRect.position);
                pointerOffset = new Vector2(
                    localToken.x - localPointer.x, localToken.y - localPointer.y);
            }
            else pointerOffset = Vector2.zero;
            UpdateDragPosition(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsOwner(eventData) && isDragging) UpdateDragPosition(eventData);
        }

        public void OnEndDrag(PointerEventData eventData) => FinishGesture(eventData);
        public void OnPointerUp(PointerEventData eventData) => FinishGesture(eventData);

        private void UpdateDragPosition(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragLayer, eventData.position, eventData.pressEventCamera, out Vector2 localPointer))
                return;
            Vector2 position = localPointer + pointerOffset;
            tokenRect.position = dragLayer.TransformPoint(new Vector3(position.x, position.y, 0f));
        }

        private bool IsOwner(PointerEventData eventData) =>
            isHeld && eventData.pointerId == ownerPointerId;

        private void FinishGesture(PointerEventData eventData)
        {
            if (!IsOwner(eventData)) return;
            string tokenId = TokenId;
            int pointerId = ownerPointerId;
            bool wasDragged = isDragging;
            Vector2 position = eventData.position;
            Camera camera = eventData.pressEventCamera;

            bool accepted = board.FinishTokenGesture(
                tokenId, pointerId, position, camera, wasDragged);

            if (!accepted && wasDragged && originalParent != null)
            {
                isHeld = false;
                isReturning = true;
                backgroundImage.raycastTarget = false;
                motion.ReturnTo(originalWorldPosition, () =>
                {
                    if (this == null) return;
                    RestoreVisualPosition();
                    board?.Refresh();
                });
            }
            else RestoreVisualPosition();
        }

        public void ForceCancel()
        {
            motion?.CancelReturn();
            if (!isHeld && !isDragging && !isReturning) return;
            if (isHeld) board?.CancelTokenGesture(TokenId, ownerPointerId);
            RestoreVisualPosition();
        }

        private void RestoreVisualPosition()
        {
            if (isDragging || isReturning) motion?.OnDragStarted();
            if (isDragging)
            {
                if (originalParent != null)
                {
                    tokenRect.SetParent(originalParent, false);
                    tokenRect.SetSiblingIndex(originalSiblingIndex);
                    tokenRect.localScale = Vector3.one;
                    tokenRect.localRotation = Quaternion.identity;
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
            isReturning = false;
            originalParent = null;
            pointerOffset = Vector2.zero;
        }

        private void OnDisable() => ForceCancel();
        private void OnApplicationPause(bool paused) { if (paused) ForceCancel(); }
        private void OnApplicationFocus(bool focus) { if (!focus) ForceCancel(); }

        private static string GetIngredientLabel(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.Carrot: return "ZAN";
                case IngredientType.Potato: return "PAT";
                case IngredientType.Onion: return "CEB";
                case IngredientType.Mushroom: return "SET";
                case IngredientType.Tomato: return "TOM";
                case IngredientType.Pumpkin: return "CAL";
                case IngredientType.Corn: return "MAIZ";
                case IngredientType.Leek: return "PUE";
                default: return "?";
            }
        }
    }
}
