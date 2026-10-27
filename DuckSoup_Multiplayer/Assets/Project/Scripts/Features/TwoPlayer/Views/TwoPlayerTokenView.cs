using System;
using DuckSoup.Features.Round.Contracts;
using DuckSoup.Features.Round.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

namespace DuckSoup.Features.TwoPlayer.Views
{
    /// <summary>Vista del token: solo entrada tactil y movimiento visual.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class TwoPlayerTokenView : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private TextMeshProUGUI ingredientText;

        private TwoPlayerBoardView board;
        private RectTransform dragLayer;
        private RectTransform rect;
        private Image image;
        private Transform homeParent;
        private int homeSiblingIndex;
        private Vector2 pointerOffset;
        private int ownerPointerId;
        private bool held;
        private bool dragging;

        public string TokenId { get; private set; }

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            image = GetComponent<Image>();
        }

        public void Initialize(TwoPlayerBoardView boardView, RectTransform layer)
        {
            board = boardView ?? throw new ArgumentNullException(nameof(boardView));
            dragLayer = layer ?? throw new ArgumentNullException(nameof(layer));
            if (ingredientText == null)
                throw new InvalidOperationException("Asigna IngredientText al prefab TwoPlayerToken.");
        }

        public void Refresh(IngredientTokenSnapshot snapshot)
        {
            if (snapshot == null)
            {
                TokenId = null;
                image.enabled = false;
                image.raycastTarget = false;
                ingredientText.text = "";
                return;
            }

            TokenId = snapshot.Id;
            image.enabled = true;
            image.raycastTarget = !snapshot.IsConsumed;
            if (snapshot.IsConsumed)
            {
                image.color = Color.gray;
                ingredientText.text = "OK";
            }
            else if (snapshot.IsRevealed)
            {
                image.color = new Color(0.95f, 0.72f, 0.35f);
                ingredientText.text = LabelFor(snapshot.VisibleIngredient);
            }
            else
            {
                image.color = new Color(0.60f, 0.37f, 0.19f);
                ingredientText.text = "?";
            }
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (held || board == null || string.IsNullOrEmpty(TokenId) ||
                data.button != PointerEventData.InputButton.Left)
                return;

            if (!board.TryBeginToken(TokenId, data.pointerId)) return;
            ownerPointerId = data.pointerId;
            held = true;
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (!IsOwner(data) || dragging) return;
            homeParent = rect.parent;
            homeSiblingIndex = rect.GetSiblingIndex();

            // El padre TokenSlot permanece en el Grid mientras la ficha viaja.
            rect.SetParent(dragLayer, true);
            rect.SetAsLastSibling();
            dragging = true;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dragLayer, data.position, data.pressEventCamera, out Vector2 localPointer))
            {
                Vector3 localToken = dragLayer.InverseTransformPoint(rect.position);
                pointerOffset = new Vector2(
                    localToken.x - localPointer.x, localToken.y - localPointer.y);
            }
            else pointerOffset = Vector2.zero;

            UpdatePosition(data);
        }

        public void OnDrag(PointerEventData data)
        {
            if (IsOwner(data) && dragging) UpdatePosition(data);
        }

        public void OnEndDrag(PointerEventData data) => Finish(data);
        public void OnPointerUp(PointerEventData data) => Finish(data);

        private void UpdatePosition(PointerEventData data)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dragLayer, data.position, data.pressEventCamera, out Vector2 localPointer))
                return;
            Vector2 localTarget = localPointer + pointerOffset;
            rect.position = dragLayer.TransformPoint(
                new Vector3(localTarget.x, localTarget.y, 0f));
        }

        private bool IsOwner(PointerEventData data) =>
            held && data.pointerId == ownerPointerId;

        private void Finish(PointerEventData data)
        {
            if (!IsOwner(data)) return;
            string id = TokenId;
            int pointer = ownerPointerId;
            bool wasDragged = dragging;
            Vector2 screenPosition = data.position;
            Camera camera = data.pressEventCamera;

            RestorePosition();
            board.FinishTokenGesture(id, pointer, screenPosition, camera, wasDragged);
        }

        public void ForceCancel()
        {
            if (!held) return;
            string id = TokenId;
            int pointer = ownerPointerId;
            RestorePosition();
            board?.CancelTokenGesture(id, pointer);
        }

        private void RestorePosition()
        {
            if (dragging && homeParent != null)
            {
                rect.SetParent(homeParent, false);
                rect.SetSiblingIndex(homeSiblingIndex);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
            }
            held = false;
            dragging = false;
            homeParent = null;
            pointerOffset = Vector2.zero;
        }

        private void OnDisable() => ForceCancel();
        private void OnApplicationPause(bool paused) { if (paused) ForceCancel(); }
        private void OnApplicationFocus(bool focus) { if (!focus) ForceCancel(); }

        private static string LabelFor(IngredientType type)
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
