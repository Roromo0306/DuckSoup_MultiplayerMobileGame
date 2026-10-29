using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuckSoup.Features.GameFeel.UI
{
    // Se aplica a cualquier Button uGUI. No modifica su onClick.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class ButtonPressFeel : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField, Range(1f, 1.3f)] private float pressedScale = 1.1f;
        [SerializeField, Min(0.01f)] private float responseSeconds = 0.09f;

        private Vector3 baseScale;
        private Vector3 targetScale;
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            baseScale = transform.localScale;
            targetScale = baseScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!button.IsActive() || !button.IsInteractable() ||
                eventData.button != PointerEventData.InputButton.Left)
                return;

            targetScale = baseScale * pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            targetScale = baseScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = baseScale;
        }

        private void Update()
        {
            if (button != null && (!button.IsActive() || !button.IsInteractable()))
            {
                    targetScale = baseScale;
            }
            float speed = Vector3.Distance(baseScale, baseScale * pressedScale) /
                          Mathf.Max(0.01f, responseSeconds);
            transform.localScale = Vector3.MoveTowards(
                transform.localScale, targetScale, speed * Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            targetScale = baseScale;
            transform.localScale = baseScale;
        }
    }
}
