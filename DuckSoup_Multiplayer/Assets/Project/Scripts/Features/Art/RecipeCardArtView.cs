using System.Collections.Generic;
using DuckSoup.Features.Round.Contracts;
using DuckSoup.Features.Round.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.Art
{
    // Solo pinta: no decide requisitos ni consume tokens.
    [DisallowMultipleComponent]
    public sealed class RecipeCardArtView : MonoBehaviour
    {
        [SerializeField] private IngredientArtLibrary ingredients;
        [SerializeField] private Image frontBackground;
        [SerializeField] private Image backBackground;
        [SerializeField] private Image[] ingredientIcons = new Image[3];
        [SerializeField] private Image[] completedChecks = new Image[3];
        [SerializeField] private bool hideLegacyTexts = true;

        private void Awake()
        {
            if (hideLegacyTexts)
                foreach (TMP_Text label in GetComponentsInChildren<TMP_Text>(true))
                    label.gameObject.SetActive(false);
        }

        public void Render(RecipeCardSnapshot snapshot)
        {
            if (snapshot == null) return;
            var types = new List<IngredientType>(3);
            var checkedSlots = new List<bool>(3);
            bool complete = true;

            foreach (IngredientRequirementSnapshot requirement in snapshot.Requirements)
            {
                int count = requirement.RequiredCount;
                int done = requirement.CompletedCount;
                if (done < count) complete = false;
                for (int i = 0; i < count; i++)
                {
                    types.Add(requirement.Ingredient);
                    checkedSlots.Add(i < done);
                }
            }
            if (types.Count != 3) complete = false;

            if (frontBackground != null) frontBackground.enabled = !complete;
            if (backBackground != null) backBackground.enabled = complete;
            for (int i = 0; i < 3; i++)
            {
                bool showIcon = !complete && i < types.Count && ingredients != null;
                if (ingredientIcons != null && i < ingredientIcons.Length && ingredientIcons[i] != null)
                {
                    Image icon = ingredientIcons[i];
                    icon.sprite = showIcon ? ingredients.Get(types[i]) : null;
                    icon.enabled = showIcon && icon.sprite != null;
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                }
                if (completedChecks != null && i < completedChecks.Length && completedChecks[i] != null)
                {
                    completedChecks[i].enabled = !complete && i < checkedSlots.Count && checkedSlots[i];
                    completedChecks[i].raycastTarget = false;
                }
            }
        }
    }
}
