using DuckSoup.Features.Audio;
using DuckSoup.Features.PowerUps;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuckSoup.Features.Conveyor
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ConveyorFlyingRecipeToken : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        public enum TrajectoryMode
        {
            Mixed,
            Straight,
            Wave
        }

        [Header("Gameplay")]
        [SerializeField] private ConveyorBeltTable conveyorBelt;
        [SerializeField] private PowerUpHud powerUpHud;

        [Header("Route between the two belts")]
        [SerializeField] private RectTransform leftPoint;
        [SerializeField] private RectTransform rightPoint;
        [SerializeField] private RectTransform dragLayer;

        [Header("Frequency")]
        [SerializeField, Min(0.2f)]
        private float intervalAtStart = 12f;

        [SerializeField, Min(0.2f)]
        private float intervalAtEnd = 3f;

        [Header("Movement")]
        [SerializeField, Min(0.15f)]
        private float crossingSeconds = 1.4f;

        [SerializeField, Min(0.05f)]
        private float returnSeconds = 0.15f;

        [Header("Trajectory")]
        [SerializeField]
        private TrajectoryMode trajectoryMode =
            TrajectoryMode.Mixed;

        [SerializeField, Min(0f)]
        private float waveAmplitude = 80f;

        [SerializeField, Range(1, 5)]
        private int waveCycles = 3;

        [Header("Pause while these objects are visible")]
        [SerializeField] private GameObject countdownOrTimeUpOverlay;
        [SerializeField] private GameObject resultsPanel;

        private RectTransform rect;
        private CanvasGroup group;

        private Transform homeParent;
        private int homeSiblingIndex;

        private Vector3 originalScale;
        private Quaternion originalRotation;

        private Vector2 pointerOffset;
        private Vector3 returnFrom;

        private float waitProgress;
        private float flightElapsed;
        private float returnElapsed;

        private int ownerPointerId;

        private bool prepared;
        private bool flying;
        private bool fromLeft;
        private bool nextFromLeft;

        private bool waveThisFlight;
        private float waveSign = 1f;

        private int mixedPathsLeft;
        private bool nextMixedIsWave;

        private bool held;
        private bool dragging;
        private bool returning;

        private bool appPaused;
        private bool appFocused = true;

        private bool CanRun =>
            prepared &&
            isActiveAndEnabled &&
            !appPaused &&
            appFocused &&
            conveyorBelt.IsConveyorActive &&
            powerUpHud.BonusInputAvailable &&
            (countdownOrTimeUpOverlay == null ||
             !countdownOrTimeUpOverlay.activeInHierarchy) &&
            (resultsPanel == null ||
             !resultsPanel.activeInHierarchy);

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            group = GetComponent<CanvasGroup>();

            if (conveyorBelt == null ||
                powerUpHud == null ||
                leftPoint == null ||
                rightPoint == null ||
                dragLayer == null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;

                Debug.LogError(
                    "[FlyingRecipe] Asigna Conveyor Belt, Power Up Hud, " +
                    "Left Point, Right Point y Drag Layer.",
                    this);

                enabled = false;
                return;
            }

            if (leftPoint == rightPoint ||
                leftPoint.IsChildOf(transform) ||
                rightPoint.IsChildOf(transform) ||
                dragLayer.IsChildOf(transform))
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;

                Debug.LogError(
                    "[FlyingRecipe] Los puntos y Drag Layer deben estar " +
                    "fuera del token; los dos puntos deben ser diferentes.",
                    this);

                enabled = false;
                return;
            }

            homeParent = rect.parent;
            homeSiblingIndex = rect.GetSiblingIndex();

            originalScale = rect.localScale;
            originalRotation = rect.localRotation;

            nextFromLeft = Random.value >= 0.5f;

            Image image = GetComponent<Image>();

            image.enabled = true;
            image.raycastTarget = true;
            image.preserveAspect = true;

            prepared = true;

            ResetFlight();
        }

        private void Update()
        {
            if (!prepared)
                return;

            if (!conveyorBelt.IsConveyorActive ||
                !powerUpHud.HasLiveSession)
            {
                ResetFlight();
                return;
            }

            if (!CanRun || Time.deltaTime <= 0f)
            {
                CancelHold();
                group.blocksRaycasts = false;
                return;
            }

            if (flying)
            {
                group.blocksRaycasts =
                    !dragging && !returning;

                if (held)
                    return;

                if (returning)
                {
                    returnElapsed += Time.deltaTime;

                    float p = Mathf.Clamp01(
                        returnElapsed /
                        Mathf.Max(0.05f, returnSeconds));

                    float eased =
                        1f -
                        (1f - p) *
                        (1f - p) *
                        (1f - p);

                    rect.position = Vector3.Lerp(
                        returnFrom,
                        FlightPosition(),
                        eased);

                    if (p >= 1f)
                        returning = false;

                    return;
                }

                flightElapsed += Time.deltaTime;
                rect.position = FlightPosition();

                if (flightElapsed >=
                    Mathf.Max(0.15f, crossingSeconds))
                {
                    ResetFlight();
                }

                return;
            }

            float interval = Mathf.Max(
                0.2f,
                Mathf.Lerp(
                    intervalAtStart,
                    intervalAtEnd,
                    powerUpHud.SessionProgress01));

            waitProgress += Time.deltaTime / interval;

            if (waitProgress >= 1f)
                BeginFlight();
        }

        private void BeginFlight()
        {
            waitProgress = 0f;
            flightElapsed = 0f;

            flying = true;

            fromLeft = nextFromLeft;
            nextFromLeft = !nextFromLeft;

            SelectTrajectory();

            rect.position = FlightPosition();

            group.alpha = 1f;
            group.blocksRaycasts = true;
        }

        private void SelectTrajectory()
        {
            if (trajectoryMode == TrajectoryMode.Straight)
            {
                waveThisFlight = false;
            }
            else if (trajectoryMode == TrajectoryMode.Wave)
            {
                waveThisFlight = true;
            }
            else
            {
                // Cada pareja tiene un recorrido recto
                // y otro ondulado, en orden aleatorio.
                if (mixedPathsLeft <= 0)
                {
                    nextMixedIsWave = Random.value < 0.5f;
                    mixedPathsLeft = 2;
                }

                waveThisFlight = nextMixedIsWave;
                nextMixedIsWave = !nextMixedIsWave;

                mixedPathsLeft--;
            }

            // La onda puede comenzar subiendo o bajando.
            waveSign = Random.value < 0.5f ? -1f : 1f;
        }

        private Vector3 FlightPosition()
        {
            float t = Mathf.Clamp01(
                flightElapsed /
                Mathf.Max(0.15f, crossingSeconds));

            Vector3 position = Vector3.Lerp(
                fromLeft
                    ? leftPoint.position
                    : rightPoint.position,
                fromLeft
                    ? rightPoint.position
                    : leftPoint.position,
                t);

            if (!waveThisFlight || t <= 0f || t >= 1f)
                return position;

            float wave =
                Mathf.Sin(
                    t *
                    Mathf.PI *
                    2f *
                    Mathf.Max(1f, waveCycles)) *
                Mathf.Max(0f, waveAmplitude) *
                waveSign;

            Vector3 localOffset =
                new Vector3(0f, wave, 0f);

            // La amplitud utiliza unidades del Canvas.
            // Se mantiene el mismo recorrido durante el arrastre.
            Vector3 worldOffset = homeParent != null
                ? homeParent.TransformVector(localOffset)
                : localOffset;

            return position + worldOffset;
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (!CanRun ||
                !flying ||
                held ||
                returning ||
                data.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            ownerPointerId = data.pointerId;
            held = true;

            rect.localScale = originalScale * 1.12f;

            DuckSoupSoundPlayer.Play(
                DuckSoupSoundId.IngredientTap);
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (!IsOwner(data) || dragging)
                return;

            if (!CanRun)
            {
                CancelHold();
                return;
            }

            rect.localScale = originalScale;

            rect.SetParent(dragLayer, true);
            rect.SetAsLastSibling();

            dragging = true;
            group.blocksRaycasts = false;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragLayer,
                data.position,
                data.pressEventCamera,
                out Vector2 pointer))
            {
                Vector3 local =
                    dragLayer.InverseTransformPoint(rect.position);

                pointerOffset = new Vector2(
                    local.x - pointer.x,
                    local.y - pointer.y);
            }
            else
            {
                pointerOffset = Vector2.zero;
            }

            UpdateDrag(data);
        }

        public void OnDrag(PointerEventData data)
        {
            if (!IsOwner(data) || !dragging)
                return;

            if (!CanRun)
            {
                CancelHold();
                return;
            }

            UpdateDrag(data);
        }

        private void UpdateDrag(PointerEventData data)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragLayer,
                data.position,
                data.pressEventCamera,
                out Vector2 pointer))
            {
                return;
            }

            Vector2 local = pointer + pointerOffset;

            rect.position = dragLayer.TransformPoint(
                new Vector3(local.x, local.y, 0f));
        }

        public void OnPointerUp(PointerEventData data)
        {
            FinishGesture(data);
        }

        public void OnEndDrag(PointerEventData data)
        {
            FinishGesture(data);
        }

        private bool IsOwner(PointerEventData data)
        {
            return held &&
                   data.pointerId == ownerPointerId;
        }

        private void FinishGesture(PointerEventData data)
        {
            if (!IsOwner(data))
                return;

            bool wasDragged = dragging;
            bool allowed = CanRun;

            Vector3 releasedAt = rect.position;

            held = false;
            dragging = false;

            bool accepted =
                wasDragged &&
                allowed &&
                powerUpHud.TryCompleteFlyingRecipe(
                    data.position,
                    data.pressEventCamera);

            if (accepted)
            {
                ResetFlight();
                return;
            }

            RestoreParent();

            if (wasDragged && allowed)
            {
                returning = true;
                returnElapsed = 0f;
                returnFrom = releasedAt;

                rect.position = releasedAt;
                group.blocksRaycasts = false;
            }
            else
            {
                rect.position = FlightPosition();
            }
        }

        private void RestoreParent()
        {
            if (homeParent != null &&
                rect.parent != homeParent)
            {
                rect.SetParent(homeParent, true);
                rect.SetSiblingIndex(homeSiblingIndex);
            }

            rect.localScale = originalScale;
            rect.localRotation = originalRotation;
        }

        private void CancelHold()
        {
            if (!held && !dragging && !returning)
                return;

            held = false;
            dragging = false;
            returning = false;

            RestoreParent();

            if (flying)
                rect.position = FlightPosition();
        }

        private void ResetFlight()
        {
            held = false;
            dragging = false;
            returning = false;
            flying = false;

            waitProgress = 0f;
            flightElapsed = 0f;

            RestoreParent();

            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        private void OnDisable()
        {
            if (prepared)
                ResetFlight();
        }

        private void OnApplicationPause(bool paused)
        {
            appPaused = paused;

            if (paused && prepared)
                CancelHold();
        }

        private void OnApplicationFocus(bool focused)
        {
            appFocused = focused;

            if (!focused && prepared)
                CancelHold();
        }
    }
}