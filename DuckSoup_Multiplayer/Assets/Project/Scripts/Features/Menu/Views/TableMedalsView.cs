using System;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.Menu.Views
{
    [DisallowMultipleComponent]
    public sealed class TableMedalsView : MonoBehaviour
    {
        [Serializable]
        private sealed class MedalImages
        {
            [SerializeField] private Image lockedImage;
            [SerializeField] private Image unlockedImage;

            public void Render(bool unlocked)
            {
                SetVisible(lockedImage, !unlocked);
                SetVisible(unlockedImage, unlocked);
            }

            private static void SetVisible(Image image, bool visible)
            {
                if (image == null)
                    return;

                image.gameObject.SetActive(true);
                image.raycastTarget = false;
                image.enabled = visible;
            }
        }

        [Header("Bronze")]
        [SerializeField] private MedalImages bronze = new MedalImages();

        [Header("Silver")]
        [SerializeField] private MedalImages silver = new MedalImages();

        [Header("Gold")]
        [SerializeField] private MedalImages gold = new MedalImages();

        [Header("Master")]
        [SerializeField] private MedalImages master = new MedalImages();

        private void Awake()
        {
            Render(null, 0);
        }

        public void Render(TableDisplayData table, int highScore)
        {
            bronze?.Render(
                table != null && highScore >= table.BronzeScore);

            silver?.Render(
                table != null && highScore >= table.SilverScore);

            gold?.Render(
                table != null && highScore >= table.GoldScore);

            master?.Render(
                table != null && highScore >= table.MasterScore);
        }
    }
}