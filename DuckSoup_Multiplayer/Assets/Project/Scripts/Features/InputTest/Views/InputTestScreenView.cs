using DuckSoup.Features.InputTest.Contracts;
using UnityEngine;

namespace DuckSoup.Features.InputTest.Views
{
  
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class InputTestScreenView : MonoBehaviour, IInputTestScreen
    {
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();

            SetInteractionEnabled(false);
        }

        public void SetInteractionEnabled(bool enabled)
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            canvasGroup.interactable = enabled;
            canvasGroup.blocksRaycasts = enabled;
        }
    }
}
