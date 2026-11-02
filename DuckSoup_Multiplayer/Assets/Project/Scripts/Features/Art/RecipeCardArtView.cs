
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
        [Header("Ingredient Art")]
        [SerializeField]
        private IngredientArtLibrary ingredients;

        [Header("Card Sides")]
        [SerializeField]
        private Image frontBackground;

        [SerializeField]
        private Image backBackground;

        [Header("Ingredient Icons")]
        [SerializeField]
        private Image[] ingredientIcons = new Image[3];

        [Header("Completed Checks")]
        [SerializeField]
        private Image[] completedChecks = new Image[3];

        [Header("Automatic Layout")]
        [SerializeField]
        private bool arrangeIconsAutomatically = true;

        [SerializeField, Range(0.3f, 0.95f)]
        private float iconWidthFraction = 0.78f;

        [SerializeField, Range(0.15f, 0.32f)]
        private float iconHeightFraction = 0.27f;

        [Header("Check Settings")]
        [SerializeField, Range(0.5f, 1.5f)]
        private float checkScale = 1f;

        [Header("Legacy UI")]
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
            {
                ArrangeVisualElements();
            }
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

                if (icon == null)
                    continue;

                RectTransform iconRect = icon.rectTransform;

                float halfWidth =
                    iconWidthFraction * 0.5f;

                float halfHeight =
                    iconHeightFraction * 0.5f;

                float y = VerticalCenters[i];

                // Posicion del ingrediente.
                iconRect.anchorMin = new Vector2(
                    0.5f - halfWidth,
                    y - halfHeight);

                iconRect.anchorMax = new Vector2(
                    0.5f + halfWidth,
                    y + halfHeight);

                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;

                iconRect.localRotation = Quaternion.identity;
                iconRect.localScale = Vector3.one;

                icon.preserveAspect = true;
                icon.raycastTarget = false;

                // El tick utiliza exactamente
                // la misma posicion y dimensiones.
                Image check = GetImage(completedChecks, i);

                if (check == null)
                    continue;

                RectTransform checkRect =
                    check.rectTransform;

                checkRect.anchorMin = iconRect.anchorMin;
                checkRect.anchorMax = iconRect.anchorMax;

                checkRect.pivot = iconRect.pivot;

                checkRect.offsetMin = iconRect.offsetMin;
                checkRect.offsetMax = iconRect.offsetMax;

                checkRect.localRotation = Quaternion.identity;

                // Podemos modificar ligeramente
                // el tamaño desde el Inspector.
                checkRect.localScale =
                    Vector3.one * checkScale;

                check.preserveAspect = true;
                check.raycastTarget = false;
            }
        }
    }
}
