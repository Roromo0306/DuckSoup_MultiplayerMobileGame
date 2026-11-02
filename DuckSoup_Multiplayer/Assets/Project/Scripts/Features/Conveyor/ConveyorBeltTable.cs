using System;
using System.Collections.Generic;
using DuckSoup.Features.SinglePlayer.Views;
using DuckSoup.Features.TwoPlayer.Views;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.Conveyor
{
    /// <summary>
    /// Presentacion de dos cintas. Mueve SLOTs con separacion fija.
    /// No modifica ingredientes, recetas ni puntuaciones.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ConveyorBeltTable : MonoBehaviour
    {
        [Header("Position markers (UI Empty / RectTransform)")]
        [SerializeField] private RectTransform upperBeltAnchor;
        [SerializeField] private RectTransform lowerBeltAnchor;

        [Header("Belt appearance")]
        [SerializeField] private Sprite beltSprite;
        [SerializeField] private Color upperBeltColor =
            new Color(0.40f, 0.69f, 0.81f, 0.60f);
        [SerializeField] private Color lowerBeltColor =
            new Color(0.87f, 0.59f, 0.33f, 0.60f);

        [Header("Movement")]
        [SerializeField, Min(1f)] private float speed = 90f;
        [SerializeField, Min(0f)] private float horizontalGap = 18f;
        [SerializeField, Range(1f, 1.5f)]
        private float maximumTokenVisualScale = 1.2f;
        [SerializeField, Range(0.1f, 0.35f)]
        private float laneVerticalOffset = 0.21f;
        [SerializeField, Min(0f)] private float beltExtraHeight = 26f;

        [Header("Optional: pause during countdown / results")]
        [SerializeField] private GameObject countdownOrTimeUpOverlay;
        [SerializeField] private GameObject resultsPanel;

        private sealed class SlotInfo
        {
            public RectTransform Slot;
            public RectTransform Token;
        }

        private sealed class Lane
        {
            public RectTransform Window;
            public readonly List<SlotInfo> Slots =
                new List<SlotInfo>();
            public int Direction;
            public float Edge;
            public float TrackLength;
            public float WrapBoundary;
        }

        private readonly Lane upper = new Lane { Direction = 1 };
        private readonly Lane lower = new Lane { Direction = -1 };

        private RectTransform root;
        private GridLayoutGroup grid;
        private Vector2 tokenSize;
        private bool useConveyor;
        private bool built;

        public bool IsConveyorActive => useConveyor && built;

        // Se llama desde el Bootstrap en Awake, antes de Start.
        public void Configure(bool enabledForThisRound)
        {
            useConveyor = enabledForThisRound;
        }

        private void Start()
        {
            if (useConveyor)
                Build();
        }

        private void Build()
        {
            if (built)
                return;

            root = GetComponent<RectTransform>();
            grid = GetComponent<GridLayoutGroup>();

            if (grid == null)
            {
                Debug.LogError(
                    "[Conveyor] TokensRoot necesita GridLayoutGroup.", this);
                return;
            }

            Canvas.ForceUpdateCanvases();
            tokenSize = grid.cellSize;

            if (tokenSize.x <= 0f || tokenSize.y <= 0f)
            {
                Debug.LogError(
                    "[Conveyor] Grid Cell Size debe ser positivo.", this);
                return;
            }

            // Recogemos los slots ANTES de crear las cintas.
            // 1P: fichas directamente bajo TokensRoot.
            // 2P: TokenSlot con ficha dentro.
            var originalSlots = new List<RectTransform>();
            foreach (Transform child in root)
            {
                if (child.GetComponent<SinglePlayerTokenView>() != null ||
                    child.GetComponent<TwoPlayerTokenView>() != null ||
                    child.GetComponentInChildren<SinglePlayerTokenView>(true) != null ||
                    child.GetComponentInChildren<TwoPlayerTokenView>(true) != null)
                {
                    originalSlots.Add((RectTransform)child);
                }
            }

            if (originalSlots.Count < 2)
            {
                Debug.LogError(
                    "[Conveyor] No se encontraron fichas en TokensRoot.", this);
                return;
            }

            // El Grid se usa solo en la mesa clasica.
            grid.enabled = false;

            float upperY = ResolveMarkerPosition(upperBeltAnchor, true).y;
            float lowerY = ResolveMarkerPosition(lowerBeltAnchor, false).y;
            float minLaneDistance =
                tokenSize.y * maximumTokenVisualScale + horizontalGap;

            if (Mathf.Abs(upperY - lowerY) < minLaneDistance)
            {
                // Evita superposicion vertical incluso si los markers
                // fueron colocados demasiado cerca.
                lowerY = upperY +
                    (lowerY >= upperY ? 1f : -1f) * minLaneDistance;
                Debug.LogWarning(
                    "[Conveyor] Los Empty estaban demasiado cerca. " +
                    "He separado automaticamente la segunda cinta.", this);
            }

            upper.Window = CreateLaneWindow(
                "Conveyor_Upper", upperBeltAnchor, upperY,
                upperBeltColor);
            lower.Window = CreateLaneWindow(
                "Conveyor_Lower", lowerBeltAnchor, lowerY,
                lowerBeltColor);

            for (int i = 0; i < originalSlots.Count; i++)
            {
                RectTransform original = originalSlots[i];
                Lane lane = (i % 2 == 0) ? upper : lower;

                SinglePlayerTokenView direct1P =
                    original.GetComponent<SinglePlayerTokenView>();
                TwoPlayerTokenView direct2P =
                    original.GetComponent<TwoPlayerTokenView>();

                RectTransform slot;
                RectTransform tokenRect;

                if (direct1P != null || direct2P != null)
                {
                    // En 1P creamos un slot para cada ficha.
                    GameObject wrapper = new GameObject(
                        $"ConveyorSlot_{i:00}", typeof(RectTransform));
                    slot = wrapper.GetComponent<RectTransform>();
                    slot.SetParent(lane.Window, false);
                    original.SetParent(slot, false);
                    tokenRect = original;
                }
                else
                {
                    // En 2P ya existen TokenSlot: los reutilizamos.
                    slot = original;
                    slot.SetParent(lane.Window, false);
                    SinglePlayerTokenView child1P =
                        slot.GetComponentInChildren<SinglePlayerTokenView>(true);
                    TwoPlayerTokenView child2P =
                        slot.GetComponentInChildren<TwoPlayerTokenView>(true);
                    tokenRect = child1P != null
                        ? child1P.GetComponent<RectTransform>()
                        : child2P != null
                            ? child2P.GetComponent<RectTransform>()
                            : null;
                }

                if (tokenRect == null)
                    continue;

                slot.anchorMin = new Vector2(0.5f, 0.5f);
                slot.anchorMax = new Vector2(0.5f, 0.5f);
                slot.pivot = new Vector2(0.5f, 0.5f);
                slot.sizeDelta = tokenSize;
                slot.localScale = Vector3.one;
                slot.localRotation = Quaternion.identity;

                StretchToken(tokenRect);
                lane.Slots.Add(new SlotInfo
                {
                    Slot = slot,
                    Token = tokenRect
                });

                // El guard anterior congelaba un solo hueco y
                // ocasionaba los solapamientos. Ya no se utiliza.
                ConveyorTokenHoldGuard oldGuard =
                    tokenRect.GetComponent<ConveyorTokenHoldGuard>();
                if (oldGuard != null)
                    oldGuard.enabled = false;
            }

            ArrangeLane(upper);
            ArrangeLane(lower);
            built = true;
            Debug.Log(
                $"[Conveyor] Dos cintas con {originalSlots.Count} fichas.",
                this);
        }

        private RectTransform CreateLaneWindow(
            string name, RectTransform marker, float centerY,
            Color color)
        {
            GameObject go = new GameObject(
                name, typeof(RectTransform), typeof(Image),
                typeof(RectMask2D));

            RectTransform window = go.GetComponent<RectTransform>();
            window.SetParent(root, false);
            window.anchorMin = new Vector2(0.5f, 0.5f);
            window.anchorMax = new Vector2(0.5f, 0.5f);
            window.pivot = new Vector2(0.5f, 0.5f);

            float width = root.rect.width;
            if (marker != null &&
                marker.rect.width >= tokenSize.x * 2f + horizontalGap)
            {
                width = marker.rect.width;
            }

            float height = Mathf.Max(
                tokenSize.y + beltExtraHeight,
                tokenSize.y * maximumTokenVisualScale + 8f);

            window.sizeDelta = new Vector2(width, height);

            Vector2 position = ResolveMarkerPosition(
                marker, name == "Conveyor_Upper");
            position.y = centerY;
            window.anchoredPosition = position;

            Image background = go.GetComponent<Image>();
            background.sprite = beltSprite;
            background.type = Image.Type.Simple;
            background.color = color;
            background.raycastTarget = false;

            // La mascara recorta cada fila de manera independiente.
            // Se sigue pudiendo arrastrar una ficha fuera al DragLayer.
            return window;
        }

        private Vector2 ResolveMarkerPosition(
            RectTransform marker, bool isUpper)
        {
            if (marker != null)
            {
                // Un RectTransform usa coordenadas locales relativas a
                // su pivot; los slots usan anclaje central. Convertimos.
                Vector2 local = root.InverseTransformPoint(marker.position);
                return local - root.rect.center;
            }

            return new Vector2(
                0f, root.rect.height *
                (isUpper ? laneVerticalOffset : -laneVerticalOffset));
        }

        private void ArrangeLane(Lane lane)
        {
            int count = lane.Slots.Count;
            if (count == 0)
                return;

            float width = lane.Window.rect.width;

            // El tamaño del pop del token tambien cuenta para no
            // solapar las fichas mientras se revelan.
            float step = Mathf.Max(
                tokenSize.x * maximumTokenVisualScale + horizontalGap,
                (width + tokenSize.x) / count);

            lane.Edge = width * 0.5f +
                        tokenSize.x * 0.5f + horizontalGap;

            // El recorrido total debe superar todo el ancho visible.
            // De lo contrario, al hacer wrap se pueden cruzar fichas.
            lane.TrackLength = Mathf.Max(
                count * step, 2f * lane.Edge + step);
            lane.WrapBoundary = lane.Direction > 0
                ? -lane.Edge + lane.TrackLength
                : lane.Edge - lane.TrackLength;

            for (int i = 0; i < count; i++)
            {
                float x = lane.Direction > 0
                    ? -lane.Edge + i * step
                    : lane.Edge - i * step;

                lane.Slots[i].Slot.anchoredPosition = new Vector2(x, 0f);
            }
        }

        private static void StretchToken(RectTransform token)
        {
            token.anchorMin = Vector2.zero;
            token.anchorMax = Vector2.one;
            token.pivot = new Vector2(0.5f, 0.5f);
            token.offsetMin = Vector2.zero;
            token.offsetMax = Vector2.zero;
            token.localScale = Vector3.one;
            token.localRotation = Quaternion.identity;
        }

        // Compatible con la clase antigua: ya NO detenemos los huecos.
        public void SetFrozen(int slotIndex, bool frozen)
        {
        }

        private void Update()
        {
            if (!IsConveyorActive)
                return;
            if (countdownOrTimeUpOverlay != null &&
                countdownOrTimeUpOverlay.activeInHierarchy)
                return;
            if (resultsPanel != null && resultsPanel.activeInHierarchy)
                return;

            float delta = Time.deltaTime;
            if (delta <= 0f)
                return;

            MoveLane(upper, delta);
            MoveLane(lower, delta);
        }

        private void MoveLane(Lane lane, float delta)
        {
            foreach (SlotInfo entry in lane.Slots)
            {
                RectTransform slot = entry.Slot;
                if (slot == null)
                    continue;

                Vector2 position = slot.anchoredPosition;
                position.x += lane.Direction * speed * delta;

                if (lane.Direction > 0)
                {
                    while (position.x > lane.WrapBoundary)
                        position.x -= lane.TrackLength;
                }
                else
                {
                    while (position.x < lane.WrapBoundary)
                        position.x += lane.TrackLength;
                }

                slot.anchoredPosition = position;
            }
        }

        private void LateUpdate()
        {
            if (!IsConveyorActive)
                return;

            // Tras devolver una ficha a su slot, la centramos. No
            // interferimos si sigue arrastrandose en DragLayer.
            CenterLaneTokens(upper);
            CenterLaneTokens(lower);
        }

        private static void CenterLaneTokens(Lane lane)
        {
            foreach (SlotInfo entry in lane.Slots)
            {
                RectTransform slot = entry.Slot;
                RectTransform token = entry.Token;

                if (slot == null || token == null || token.parent != slot)
                    continue;

                if (token.anchorMin != Vector2.zero ||
                    token.anchorMax != Vector2.one ||
                    token.offsetMin != Vector2.zero ||
                    token.offsetMax != Vector2.zero)
                {
                    token.anchorMin = Vector2.zero;
                    token.anchorMax = Vector2.one;
                    token.offsetMin = Vector2.zero;
                    token.offsetMax = Vector2.zero;
                }
            }
        }

    }
}
