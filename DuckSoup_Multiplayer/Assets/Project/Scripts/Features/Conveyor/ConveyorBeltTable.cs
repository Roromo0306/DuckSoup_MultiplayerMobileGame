using System.Collections.Generic;
using DuckSoup.Features.SinglePlayer.Views;
using DuckSoup.Features.TwoPlayer.Views;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.Conveyor
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ConveyorBeltTable : MonoBehaviour
    {
        [Header("Position markers (UI Empty / RectTransform)")]
        [SerializeField] private RectTransform upperBeltAnchor;
        [SerializeField] private RectTransform lowerBeltAnchor;

        [Header("Belt appearance")]
        [SerializeField] private Sprite beltSprite;

        [SerializeField]
        private Color upperBeltColor =
            new Color(0.40f, 0.69f, 0.81f, 0.60f);

        [SerializeField]
        private Color lowerBeltColor =
            new Color(0.87f, 0.59f, 0.33f, 0.60f);

        [Header("Table backgrounds (optional)")]
        [SerializeField] private Sprite classicBackgroundSprite;
        [SerializeField] private Sprite conveyorBackgroundSprite;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private Image backgroundImage;

        [Header("Table music (optional)")]
        [SerializeField] private AudioSource gameplayMusicSource;
        [SerializeField] private AudioClip classicMusicClip;
        [SerializeField] private AudioClip conveyorMusicClip;

        [Header("Movement")]
        [SerializeField, Min(1f)] private float speed = 90f;
        [SerializeField, Min(0f)] private float horizontalGap = 18f;

        [SerializeField, Range(1f, 1.5f)]
        private float maximumTokenVisualScale = 1.2f;

        [SerializeField, Range(0.1f, 0.35f)]
        private float laneVerticalOffset = 0.21f;

        [SerializeField, Min(0f)]
        private float beltExtraHeight = 26f;

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

        private float speedMultiplier = 1f;

        public bool IsConveyorActive => useConveyor && built;

        public void Configure(bool enabledForThisRound)
        {
            useConveyor = enabledForThisRound;
            speedMultiplier = 1f;

            AudioClip music = useConveyor
                ? conveyorMusicClip
                : classicMusicClip;

            if (gameplayMusicSource != null && music != null)
                gameplayMusicSource.clip = music;

            if (classicBackgroundSprite == null ||
                conveyorBackgroundSprite == null)
            {
                return;
            }

            Sprite background = useConveyor
                ? conveyorBackgroundSprite
                : classicBackgroundSprite;

            if (backgroundRenderer != null)
                backgroundRenderer.sprite = background;

            if (backgroundImage != null)
                backgroundImage.sprite = background;
        }

        public void UpdateSpeedFromRemainingTime(float remainingSeconds)
        {
            if (!useConveyor)
            {
                speedMultiplier = 1f;
                return;
            }

            if (remainingSeconds <= 10f)
            {
                speedMultiplier = 1.65f;
            }
            else if (remainingSeconds <= 30f)
            {
                speedMultiplier = 1.5f;
            }
            else
            {
                speedMultiplier = 1f;
            }
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
                    "[Conveyor] TokensRoot necesita GridLayoutGroup.",
                    this);

                return;
            }

            Canvas.ForceUpdateCanvases();

            tokenSize = grid.cellSize;

            if (tokenSize.x <= 0f || tokenSize.y <= 0f)
            {
                Debug.LogError(
                    "[Conveyor] Grid Cell Size debe ser positivo.",
                    this);

                return;
            }

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
                    "[Conveyor] No se encontraron fichas en TokensRoot.",
                    this);

                return;
            }

            grid.enabled = false;

            float upperY =
                ResolveMarkerPosition(upperBeltAnchor, true).y;

            float lowerY =
                ResolveMarkerPosition(lowerBeltAnchor, false).y;

            float minLaneDistance =
                tokenSize.y * maximumTokenVisualScale + horizontalGap;

            if (Mathf.Abs(upperY - lowerY) < minLaneDistance)
            {
                lowerY = upperY +
                    (lowerY >= upperY ? 1f : -1f) * minLaneDistance;

                Debug.LogWarning(
                    "[Conveyor] Los Empty estaban demasiado cerca. " +
                    "He separado automaticamente la segunda cinta.",
                    this);
            }

            upper.Window = CreateLaneWindow(
                "Conveyor_Upper",
                upperBeltAnchor,
                upperY,
                upperBeltColor);

            lower.Window = CreateLaneWindow(
                "Conveyor_Lower",
                lowerBeltAnchor,
                lowerY,
                lowerBeltColor);

            for (int i = 0; i < originalSlots.Count; i++)
            {
                RectTransform original = originalSlots[i];
                Lane lane = i % 2 == 0 ? upper : lower;

                SinglePlayerTokenView direct1P =
                    original.GetComponent<SinglePlayerTokenView>();

                TwoPlayerTokenView direct2P =
                    original.GetComponent<TwoPlayerTokenView>();

                RectTransform slot;
                RectTransform tokenRect;

                if (direct1P != null || direct2P != null)
                {
                    GameObject wrapper = new GameObject(
                        $"ConveyorSlot_{i:00}",
                        typeof(RectTransform));

                    slot = wrapper.GetComponent<RectTransform>();
                    slot.SetParent(lane.Window, false);

                    original.SetParent(slot, false);
                    tokenRect = original;
                }
                else
                {
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
            string name,
            RectTransform marker,
            float centerY,
            Color color)
        {
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
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
                marker,
                name == "Conveyor_Upper");

            position.y = centerY;
            window.anchoredPosition = position;

            Image background = go.GetComponent<Image>();

            background.sprite = beltSprite;
            background.type = Image.Type.Simple;
            background.color = color;
            background.raycastTarget = false;

            return window;
        }

        private Vector2 ResolveMarkerPosition(
            RectTransform marker,
            bool isUpper)
        {
            if (marker != null)
            {
                Vector2 local =
                    root.InverseTransformPoint(marker.position);

                return local - root.rect.center;
            }

            return new Vector2(
                0f,
                root.rect.height *
                (isUpper ? laneVerticalOffset : -laneVerticalOffset));
        }

        private void ArrangeLane(Lane lane)
        {
            int count = lane.Slots.Count;

            if (count == 0)
                return;

            float width = lane.Window.rect.width;

            float step = Mathf.Max(
                tokenSize.x * maximumTokenVisualScale + horizontalGap,
                (width + tokenSize.x) / count);

            lane.Edge =
                width * 0.5f + tokenSize.x * 0.5f + horizontalGap;

            lane.TrackLength = Mathf.Max(
                count * step,
                2f * lane.Edge + step);

            lane.WrapBoundary = lane.Direction > 0
                ? -lane.Edge + lane.TrackLength
                : lane.Edge - lane.TrackLength;

            for (int i = 0; i < count; i++)
            {
                float x = lane.Direction > 0
                    ? -lane.Edge + i * step
                    : lane.Edge - i * step;

                lane.Slots[i].Slot.anchoredPosition =
                    new Vector2(x, 0f);
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

        public void SetFrozen(int slotIndex, bool frozen)
        {
        }

        private void Update()
        {
            if (!IsConveyorActive)
                return;

            if (countdownOrTimeUpOverlay != null &&
                countdownOrTimeUpOverlay.activeInHierarchy)
            {
                return;
            }

            if (resultsPanel != null &&
                resultsPanel.activeInHierarchy)
            {
                return;
            }

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

                position.x +=
                    lane.Direction * speed * speedMultiplier * delta;

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

            CenterLaneTokens(upper);
            CenterLaneTokens(lower);
        }

        private static void CenterLaneTokens(Lane lane)
        {
            foreach (SlotInfo entry in lane.Slots)
            {
                RectTransform slot = entry.Slot;
                RectTransform token = entry.Token;

                if (slot == null ||
                    token == null ||
                    token.parent != slot)
                {
                    continue;
                }

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