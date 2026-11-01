
using System.Collections.Generic;
using DuckSoup.Features.Round.Contracts;
using DuckSoup.Features.Round.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.Art
{
    [DisallowMultipleComponent]
    public sealed class RecipeCardArtView : MonoBehaviour
    {
        [Header("Ingredient art library")]
        [SerializeField]
        private IngredientArtLibrary ingredients;

        [Header("Card sides")]
        [SerializeField]
        private Image frontBackground;

        [SerializeField]
        private Image backBackground;

        [Header("Exactly three ingredient images")]
        [SerializeField]
        private Image[] ingredientIcons = new Image[3];

        [SerializeField]
        private Image[] completedChecks = new Image[3];

        [Header("Visual layout")]
        [SerializeField]
        private bool arrangeIconsAutomatically = true;

        [SerializeField, Range(0.3f, 0.95f)]
        private float iconWidthFraction = 0.78f;

        [SerializeField, Range(0.15f, 0.32f)]
        private float iconHeightFraction = 0.27f;

        [SerializeField, Range(0.1f, 0.3f)]
        private float checkWidthFraction = 0.18f;

        [SerializeField]
        private bool hideLegacyTexts = true;

        private static readonly float[] VerticalCenters =
        {
            0.78f,
            0.50f,
            0.22f
        };

        private bool textsHidden;

        private void Awake()
        {
            if (arrangeIconsAutomatically)
                ArrangeVisualElements();
        }

        public void Render(RecipeCardSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            if (hideLegacyTexts && !textsHidden)
            {
                foreach (TMP_Text label in
                         GetComponentsInChildren<TMP_Text>(true))
                {
                    label.gameObject.SetActive(false);
                }

                textsHidden = true;
            }

            var types = new List<IngredientType>(3);
            var checkedSlots = new List<bool>(3);

            bool complete = true;

            foreach (IngredientRequirementSnapshot requirement
                     in snapshot.Requirements)
            {
                if (requirement.CompletedCount <
                    requirement.RequiredCount)
                {
                    complete = false;
                }

                for (int i = 0;
                     i < requirement.RequiredCount;
                     i++)
                {
                    types.Add(requirement.Ingredient);

                    checkedSlots.Add(
                        i < requirement.CompletedCount);
                }
            }

            if (types.Count != 3)
                complete = false;

            // Mostramos la cara correspondiente.
            ShowImage(frontBackground, !complete);
            ShowImage(backBackground, complete);

            for (int i = 0; i < 3; i++)
            {
                Image icon = GetImage(ingredientIcons, i);

                if (icon != null)
                {
                    Sprite sprite =
                        !complete &&
                        i < types.Count &&
                        ingredients != null
                        ? ingredients.GetRecipe(types[i])
                        : null;

                    icon.sprite = sprite;
                    icon.preserveAspect = true;

                    ShowImage(
                        icon,
                        !complete && sprite != null);
                }

                Image check = GetImage(completedChecks, i);

                if (check != null)
                {
                    bool visible =
                        !complete &&
                        i < checkedSlots.Count &&
                        checkedSlots[i];

                    ShowImage(check, visible);
                }
            }
        }

        private static Image GetImage(
            Image[] images,
            int index)
        {
            return images != null && index < images.Length
                ? images[index]
                : null;
        }

        private static void ShowImage(
            Image image,
            bool visible)
        {
            if (image == null)
                return;

            if (!image.gameObject.activeSelf)
                image.gameObject.SetActive(true);

            image.enabled = visible;
            image.raycastTarget = false;
            image.color = Color.white;
        }

        private void ArrangeVisualElements()
        {
            for (int i = 0; i < 3; i++)
            {
                Image icon = GetImage(ingredientIcons, i);

                if (icon != null)
                {
                    RectTransform rect = icon.rectTransform;

                    float halfW =
                        iconWidthFraction * 0.5f;

                    float halfH =
                        iconHeightFraction * 0.5f;

                    float y = VerticalCenters[i];

                    rect.anchorMin = new Vector2(
                        0.5f - halfW,
                        y - halfH);

                    rect.anchorMax = new Vector2(
                        0.5f + halfW,
                        y + halfH);

                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;

                    rect.localRotation = Quaternion.identity;
                    rect.localScale = Vector3.one;

                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                }

                Image check = GetImage(completedChecks, i);

                if (check != null)
                {
                    RectTransform rect = check.rectTransform;

                    float halfW =
                        checkWidthFraction * 0.5f;

                    float y = VerticalCenters[i] + 0.08f;

                    rect.anchorMin = new Vector2(
                        0.73f - halfW,
                        y - 0.045f);

                    rect.anchorMax = new Vector2(
                        0.73f + halfW,
                        y + 0.045f);

                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;

                    rect.localRotation = Quaternion.identity;
                    rect.localScale = Vector3.one;

                    check.preserveAspect = true;
                    check.raycastTarget = false;
                }
            }
        }
    }
}
