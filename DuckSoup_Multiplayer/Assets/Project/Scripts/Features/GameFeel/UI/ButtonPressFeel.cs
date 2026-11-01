using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuckSoup.Features.GameFeel.UI
{
    // Retrasa cualquier Button.onClick (incluso listeners de Inspector)
    // hasta que el boton ha vuelto visualmente a su tamano original.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class ButtonPressFeel : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField, Range(1f, 1.3f)] private float pressedScale = 1.10f;
        [SerializeField, Min(0.01f)] private float responseSeconds = 0.09f;

        private Button button;
        private Vector3 baseScale;
        private Vector3 targetScale;
        private Button.ButtonClickedEvent originalClick;
        private Button.ButtonClickedEvent gate;
        private Coroutine pending;
        private bool installed;
        private bool forwarding;

        private void Awake()
        {
            button = GetComponent<Button>();
            baseScale = transform.localScale;
            targetScale = baseScale;
        }

        // En Start ya se han conectado los listeners que los Boostraps registran en Awake.
        private void Start()
        {
            originalClick = button.onClick;
            gate = new Button.ButtonClickedEvent();
            gate.AddListener(DelayOriginalClick);
            button.onClick = gate;
            installed = true;
        }

        public bool IsResting => Vector3.Distance(transform.localScale, baseScale) < 0.002f;
        public void ReleaseVisual() { targetScale = baseScale; }

        public void OnPointerDown(PointerEventData data)
        {
            if (button == null || !button.IsActive() || !button.IsInteractable() ||
                data.button != PointerEventData.InputButton.Left) return;
            targetScale = baseScale * pressedScale;
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left) targetScale = baseScale;
        }

        public void OnPointerExit(PointerEventData data) => targetScale = baseScale;

        private void DelayOriginalClick()
        {
            if (!installed || forwarding || pending != null || button == null ||
                !button.IsActive() || !button.IsInteractable()) return;
            targetScale = baseScale;
            pending = StartCoroutine(WaitForRestThenClick());
        }

        private IEnumerator WaitForRestThenClick()
        {
            float timeout = 0.6f;
            while (Vector3.Distance(transform.localScale, baseScale) > 0.002f && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
            transform.localScale = baseScale;
            pending = null;
            if (button == null || !button.IsActive() || !button.IsInteractable()) yield break;
            forwarding = true;
            try { originalClick?.Invoke(); }
            finally { forwarding = false; }
        }

        private void Update()
        {
            if (button != null && (!button.IsActive() || !button.IsInteractable()))
                targetScale = baseScale;
            float speed = Vector3.Distance(baseScale, baseScale * pressedScale) /
                          Mathf.Max(0.01f, responseSeconds);
            transform.localScale = Vector3.MoveTowards(
                transform.localScale, targetScale, speed * Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            if (pending != null) StopCoroutine(pending);
            pending = null;
            targetScale = baseScale;
            transform.localScale = baseScale;
        }
    }
}
