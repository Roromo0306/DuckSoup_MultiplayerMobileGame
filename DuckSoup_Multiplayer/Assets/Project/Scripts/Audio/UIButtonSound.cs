
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuckSoup.Features.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonSound :
        MonoBehaviour,
        IPointerDownHandler
    {
        [SerializeField]
        private DuckSoupSoundId sound =
            DuckSoupSoundId.ButtonClick;

        [SerializeField]
        private bool overrideAutomaticDetection;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        public void ConfigureDefault(
            DuckSoupSoundId defaultSound)
        {
            if (!overrideAutomaticDetection)
                sound = defaultSound;
        }

        public void OnPointerDown(
            PointerEventData eventData)
        {
            if (button == null ||
                !button.IsActive() ||
                !button.IsInteractable() ||
                eventData.button !=
                PointerEventData.InputButton.Left)
            {
                return;
            }

            if (sound != DuckSoupSoundId.None)
            {
                DuckSoupSoundPlayer.Play(sound);
            }
        }
    }
}
