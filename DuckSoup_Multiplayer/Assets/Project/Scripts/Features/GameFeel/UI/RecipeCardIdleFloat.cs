using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.GameFeel.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)]
    [RequireComponent(typeof(RectTransform))]
    public sealed class RecipeCardIdleFloat : MonoBehaviour
    {
        [Header("Floating Movement")]
        [SerializeField, Range(0f, 20f)]
        private float movementDistance = 6f;

        [SerializeField, Range(0f, 8f)]
        private float rotationAmount = 1.5f;

        [SerializeField, Range(0.1f, 3f)]
        private float movementSpeed = 0.85f;

        [Header("Individual Card Behaviour")]
        [SerializeField]
        private bool differentDirectionPerCard = true;

        [SerializeField]
        private Vector2 customDirection = Vector2.up;

        private static int nextCardIndex;

        // Cada carta utiliza una direccion diferente.
        private static readonly Vector2[] Directions =
        {
            new Vector2(-1f, 0.45f),
            new Vector2(0.25f, 1f),
            new Vector2(1f, -0.25f),
            new Vector2(-0.75f, -0.85f),
            new Vector2(0.95f, 0.5f),
            new Vector2(0.15f, -1f)
        };

        private RectTransform floatingPivot;
        private RectTransform layoutSlot;

        private Vector2 direction;
        private Vector2 perpendicular;

        private float phase;
        private float individualSpeed;
        private float individualRotation;

        private bool initialized;

        private void Awake()
        {
            int variant = nextCardIndex++;
            variant %= Directions.Length;

            direction = differentDirectionPerCard
                ? Directions[variant].normalized
                : customDirection.normalized;

            if (direction.sqrMagnitude < 0.001f)
                direction = Vector2.up;

            perpendicular = new Vector2(
                -direction.y,
                direction.x);

            // Desfase distinto para que no se sincronicen.
            phase = variant * 1.37f + 0.35f;

            // Cada carta tiene una velocidad ligeramente distinta.
            individualSpeed =
                movementSpeed * (0.82f + variant * 0.055f);

            // Algunas cartas giran hacia el lado contrario.
            individualRotation =
                variant % 2 == 0 ? 1f : -1f;

            CreateIndependentPivot();
        }

        private void Start()
        {
            // Respaldo por si el prefab aun no
            // tenia padre cuando se ejecuto Awake.
            if (!initialized)
                CreateIndependentPivot();
        }

        private void CreateIndependentPivot()
        {
            if (initialized)
                return;

            RectTransform card =
                GetComponent<RectTransform>();

            RectTransform originalParent =
                card.parent as RectTransform;

            if (originalParent == null)
                return;

            int originalSibling =
                card.GetSiblingIndex();

            Vector2 originalAnchorMin = card.anchorMin;
            Vector2 originalAnchorMax = card.anchorMax;
            Vector2 originalPivot = card.pivot;

            Vector2 originalSize = card.sizeDelta;
            Vector2 originalPosition = card.anchoredPosition;

            Quaternion originalRotation =
                card.localRotation;

            Vector3 originalScale =
                card.localScale;

            float originalWidth =
                Mathf.Max(
                    card.rect.width,
                    Mathf.Abs(card.sizeDelta.x));

            float originalHeight =
                Mathf.Max(
                    card.rect.height,
                    Mathf.Abs(card.sizeDelta.y));

            // 1. Crear un contenedor que conserva
            // la posicion de la carta en el layout.
            GameObject slotObject = new GameObject(
                gameObject.name + "_FloatSlot",
                typeof(RectTransform));

            layoutSlot =
                slotObject.GetComponent<RectTransform>();

            layoutSlot.SetParent(
                originalParent,
                false);

            layoutSlot.SetSiblingIndex(
                originalSibling);

            layoutSlot.anchorMin = originalAnchorMin;
            layoutSlot.anchorMax = originalAnchorMax;
            layoutSlot.pivot = originalPivot;

            layoutSlot.sizeDelta = originalSize;
            layoutSlot.anchoredPosition = originalPosition;

            layoutSlot.localRotation = Quaternion.identity;
            layoutSlot.localScale = Vector3.one;

            // Mantener los ajustes de LayoutElement,
            // si el prefab original los utilizaba.
            LayoutElement oldLayout =
                GetComponent<LayoutElement>();

            if (oldLayout != null)
            {
                LayoutElement newLayout =
                    slotObject.AddComponent<LayoutElement>();

                newLayout.ignoreLayout = oldLayout.ignoreLayout;
                newLayout.minWidth = oldLayout.minWidth;
                newLayout.minHeight = oldLayout.minHeight;
                newLayout.preferredWidth = oldLayout.preferredWidth;
                newLayout.preferredHeight = oldLayout.preferredHeight;
                newLayout.flexibleWidth = oldLayout.flexibleWidth;
                newLayout.flexibleHeight = oldLayout.flexibleHeight;
                newLayout.layoutPriority = oldLayout.layoutPriority;
            }
            else if (originalParent.GetComponent<
                HorizontalOrVerticalLayoutGroup>() != null)
            {
                LayoutElement newLayout =
                    slotObject.AddComponent<LayoutElement>();

                newLayout.preferredWidth =
                    Mathf.Max(1f, originalWidth);

                newLayout.preferredHeight =
                    Mathf.Max(1f, originalHeight);
            }

            // 2. Crear un pivote exclusivo
            // para la animacion flotante.
            GameObject pivotObject = new GameObject(
                "IdleFloatingPivot",
                typeof(RectTransform));

            floatingPivot =
                pivotObject.GetComponent<RectTransform>();

            floatingPivot.SetParent(layoutSlot, false);

            floatingPivot.anchorMin = Vector2.zero;
            floatingPivot.anchorMax = Vector2.one;
            floatingPivot.pivot = originalPivot;

            floatingPivot.offsetMin = Vector2.zero;
            floatingPivot.offsetMax = Vector2.zero;

            floatingPivot.anchoredPosition = Vector2.zero;
            floatingPivot.localRotation = Quaternion.identity;
            floatingPivot.localScale = Vector3.one;

            // 3. Colocar la carta dentro del pivote.
            // RecipeCardView y RecipeCardFeel
            // siguen en el objeto original.
            card.SetParent(floatingPivot, false);

            card.anchorMin = Vector2.zero;
            card.anchorMax = Vector2.one;
            card.pivot = originalPivot;

            card.offsetMin = Vector2.zero;
            card.offsetMax = Vector2.zero;

            card.anchoredPosition = Vector2.zero;
            card.localRotation = originalRotation;
            card.localScale = originalScale;

            initialized = true;
        }

        private void LateUpdate()
        {
            if (!initialized || floatingPivot == null)
                return;

            float time =
                Time.unscaledTime * individualSpeed;

            // Movimiento principal en una direccion.
            float mainWave = Mathf.Sin(time + phase);

            // Movimiento secundario para evitar
            // una trayectoria mecanica.
            float secondaryWave =
                Mathf.Sin(time * 0.73f + phase * 1.8f);

            Vector2 movement =
                direction * (mainWave * movementDistance);

            movement +=
                perpendicular *
                (secondaryWave * movementDistance * 0.30f);

            floatingPivot.anchoredPosition = movement;

            // Rotacion lenta y sutil.
            float rotationWave =
                Mathf.Sin(time * 0.82f + phase * 0.65f);

            float secondaryRotation =
                Mathf.Sin(time * 0.56f + phase * 1.4f);

            float angle =
                rotationAmount *
                individualRotation *
                (rotationWave + secondaryRotation * 0.18f);

            floatingPivot.localRotation =
                Quaternion.Euler(0f, 0f, angle);
        }

        private void OnDisable()
        {
            if (floatingPivot == null)
                return;

            floatingPivot.anchoredPosition = Vector2.zero;
            floatingPivot.localRotation = Quaternion.identity;
        }

        private void OnDestroy()
        {
            // Al eliminar una carta individual,
            // eliminar tambien su contenedor.
            if (UnityEngine.Application.isPlaying && layoutSlot != null)
            {
                Destroy(layoutSlot.gameObject);
            }
        }
    }
}
