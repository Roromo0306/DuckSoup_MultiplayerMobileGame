using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.UI
{
    [DisallowMultipleComponent]
    public sealed class CountdownSpriteView : MonoBehaviour
    {
        [SerializeField] private Image countdownImage;
        [SerializeField] private Sprite threeSprite;
        [SerializeField] private Sprite twoSprite;
        [SerializeField] private Sprite oneSprite;
        [SerializeField] private Sprite duckSoupSprite;

        private void Awake()
        {
            if (countdownImage != null)
            {
                countdownImage.raycastTarget = false;
                countdownImage.preserveAspect = true;
            }

            Hide();
        }

        public bool TryShow(string cue)
        {
            Sprite sprite;

            switch (cue)
            {
                case "3":
                    sprite = threeSprite;
                    break;

                case "2":
                    sprite = twoSprite;
                    break;

                case "1":
                    sprite = oneSprite;
                    break;

                case "DUCK SOUP!":
                    sprite = duckSoupSprite;
                    break;

                default:
                    Hide();
                    return false;
            }

            if (countdownImage == null || sprite == null)
            {
                Hide();
                return false;
            }

            countdownImage.sprite = sprite;
            countdownImage.color = Color.white;
            countdownImage.enabled = true;
            countdownImage.gameObject.SetActive(true);

            return true;
        }

        public void Hide()
        {
            if (countdownImage != null)
                countdownImage.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            Hide();
        }
    }
}