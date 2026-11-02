using System;
using DuckSoup.Features.Art;
using DuckSoup.Features.Conveyor;
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
        private TokenArtView art;

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
            art = GetComponent<TokenArtView>();

            if (art != null && art.IsConfigured && ingredientText != null)
                ingredientText.gameObject.SetActive(false);

            motion = GetComponent<TokenMotionFeel>();
            if (motion == null)
                motion = gameObject.AddComponent<TokenMotionFeel>();
        }

        public void Initialize(
            SinglePlayerBoardView boardView,
            RectTransform layer)
        {
            board = boardView ?? throw new ArgumentNullException(
                nameof(boardView));
            dragLayer = layer ?? throw new ArgumentNullException(
                nameof(layer));

            if (ingredientText == null && (art == null || !art.IsConfigured))
                throw new InvalidOperationException(
                    "Asigna IngredientText o TokenArtView.");
        }

        public void Refresh(IngredientTokenSnapshot snapshot)
        {
            if (snapshot == null)
            {
                TokenId = null;
                backgroundImage.enabled = false;
                backgroundImage.raycastTarget = false;
                SetLabel("");
                art?.Render(snapshot);
                return;
            }

            TokenId = snapshot.Id;
            backgroundImage.enabled = true;
            backgroundImage.raycastTarget =
                !snapshot.IsConsumed && !isReturning;

            if (snapshot.IsRevealed)
            {
                SetLabel(GetIngredientLabel(snapshot.VisibleIngredient));
                backgroundImage.color =
                    new Color(0.95f, 0.72f, 0.35f);
            }
            else
            {
                SetLabel("?");
                backgroundImage.color =
                    new Color(0.60f, 0.37f, 0.19f);
            }

            art?.Render(snapshot);
        }

        private void SetLabel(string value)
        {
            if (ingredientText != null)
                ingredientText.text = value;
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (isHeld || isReturning || board == null ||
                string.IsNullOrEmpty(TokenId) ||
                data.button != PointerEventData.InputButton.Left)
                return;

            if (!board.TryBeginToken(TokenId, data.pointerId))
                return;

            ownerPointerId = data.pointerId;
            isHeld = true;
            motion.OnRevealed();
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (!IsOwner(data) || isDragging)
                return;

            motion.OnDragStarted();
            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();
            originalWorldPosition = tokenRect.position;

            // En el Grid clasico conserva el hueco; en conveyor el
            // wrapper se mueve por separado mientras arrastramos.
            placeholder = new GameObject(
                "TokenPlaceholder", typeof(RectTransform));
            placeholder.transform.SetParent(originalParent, false);
            placeholder.transform.SetSiblingIndex(originalSiblingIndex);

            tokenRect.SetParent(dragLayer, true);
            tokenRect.SetAsLastSibling();
            isDragging = true;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragLayer, data.position, data.pressEventCamera,
                out Vector2 localPointer))
            {
                Vector3 localToken =
                    dragLayer.InverseTransformPoint(tokenRect.position);
                pointerOffset = new Vector2(
                    localToken.x - localPointer.x,
                    localToken.y - localPointer.y);
            }
            else
            {
                pointerOffset = Vector2.zero;
            }

            UpdateDragPosition(data);
        }

        public void OnDrag(PointerEventData data)
        {
            if (IsOwner(data) && isDragging)
                UpdateDragPosition(data);
        }

        public void OnEndDrag(PointerEventData data) => FinishGesture(data);
        public void OnPointerUp(PointerEventData data) => FinishGesture(data);

        private void UpdateDragPosition(PointerEventData data)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragLayer, data.position, data.pressEventCamera,
                out Vector2 localPointer))
                return;

            Vector2 position = localPointer + pointerOffset;
            tokenRect.position = dragLayer.TransformPoint(
                new Vector3(position.x, position.y, 0f));
        }

        private bool IsOwner(PointerEventData data)
        {
            return isHeld && data.pointerId == ownerPointerId;
        }

        private void FinishGesture(PointerEventData data)
        {
            if (!IsOwner(data))
                return;

            string id = TokenId;
            int pointer = ownerPointerId;
            bool wasDragged = isDragging;

            bool accepted = board.FinishTokenGesture(
                id, pointer, data.position,
                data.pressEventCamera, wasDragged);

            if (!accepted && wasDragged && originalParent != null)
            {
                isHeld = false;
                isReturning = true;
                backgroundImage.raycastTarget = false;

                // Si hay cinta, seguimos el hueco mientras avanza.
                // En la mesa clasica conservamos el retorno original.
                RectTransform movingSlot = IsConveyorHome()
                    ? originalParent as RectTransform
                    : null;

                Action finished = () =>
                {
                    if (this == null)
                        return;
                    RestoreVisualPosition();
                    board?.Refresh();
                };

                if (movingSlot != null)
                    motion.ReturnToMovingSlot(movingSlot, finished);
                else
                    motion.ReturnTo(originalWorldPosition, finished);
            }
            else
            {
                RestoreVisualPosition();
            }
        }

        private bool IsConveyorHome()
        {
            if (originalParent == null)
                return false;

            ConveyorBeltTable belt =
                originalParent.GetComponentInParent<ConveyorBeltTable>();

            return belt != null && belt.IsConveyorActive;
        }

        public void ForceCancel()
        {
            motion?.CancelReturn();

            if (!isHeld && !isDragging && !isReturning)
                return;

            if (isHeld)
                board?.CancelTokenGesture(TokenId, ownerPointerId);

            RestoreVisualPosition();
        }

        private void RestoreVisualPosition()
        {
            bool conveyor = IsConveyorHome();

            if (isDragging || isReturning)
                motion?.OnDragStarted();

            if (isDragging && originalParent != null)
            {
                tokenRect.SetParent(originalParent, false);
                tokenRect.SetSiblingIndex(originalSiblingIndex);
                tokenRect.localScale = Vector3.one;
                tokenRect.localRotation = Quaternion.identity;

                if (conveyor)
                {
                    tokenRect.anchorMin = Vector2.zero;
                    tokenRect.anchorMax = Vector2.one;
                    tokenRect.pivot = new Vector2(0.5f, 0.5f);
                    tokenRect.offsetMin = Vector2.zero;
                    tokenRect.offsetMax = Vector2.zero;
                }
            }

            if (placeholder != null)
            {
                placeholder.SetActive(false);
                Destroy(placeholder);
                placeholder = null;
            }

            isHeld = false;
            isDragging = false;
            isReturning = false;
            originalParent = null;
            pointerOffset = Vector2.zero;
        }

        private void OnDisable() => ForceCancel();
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
