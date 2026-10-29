using TMPro;
using UnityEngine;
using DuckSoup.Features.GameFeel.UI;

namespace DuckSoup.Features.GameFeel.Gameplay
{
    // Configurar sobre TMP que muestra SOLO el numero (sin "RECIPES:").
    [DisallowMultipleComponent]
    public sealed class ScoreNumberFeel : MonoBehaviour
    {
        [SerializeField] private TMP_Text numberText;
        [SerializeField] private UIPunchFeel punch;
        private int previous = int.MinValue;

        private void Awake()
        {
            if (numberText == null) numberText = GetComponent<TMP_Text>();
            if (punch == null) punch = GetComponent<UIPunchFeel>();
            if (punch == null) punch = gameObject.AddComponent<UIPunchFeel>();
        }

        public void Show(int value)
        {
            if (numberText != null) numberText.text = value.ToString();
            if (previous != int.MinValue && value > previous) punch.Punch();
            previous = value;
        }
    }
}
