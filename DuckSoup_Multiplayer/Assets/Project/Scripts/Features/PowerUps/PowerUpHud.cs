using DuckSoup.Features.SinglePlayer.Services;
using DuckSoup.Features.TwoPlayer.Services;
using TMPro;
using UnityEngine;

namespace DuckSoup.Features.PowerUps
{
    /// <summary>Optional HUD showing the combo countdown and per-player locks.</summary>
    public sealed class PowerUpHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI combo1Text;
        [SerializeField] private TextMeshProUGUI combo2Text;
        [SerializeField] private TextMeshProUGUI player1LockText;
        [SerializeField] private TextMeshProUGUI player2LockText;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField, Min(0.1f)] private float feedbackSeconds = 1.2f;

        private SinglePlayerSession single;
        private TwoPlayerSession multi;
        private float feedbackLeft;

        public void Initialize(SinglePlayerSession session) { single = session; multi = null; }
        public void Initialize(TwoPlayerSession session) { multi = session; single = null; }
        public void ShowFeedback(string message)
        {
            if (feedbackText == null) return;
            feedbackText.text = message;
            feedbackText.enabled = true;
            feedbackLeft = feedbackSeconds;
        }

        private void Update()
        {
            if (single != null)
            {
                Render(combo1Text, "COMBO x2  ", single.ComboRemaining);
            }
            if (multi != null)
            {
                Render(combo1Text, "P1 COMBO x2  ", multi.Player1ComboRemaining);
                Render(combo2Text, "P2 COMBO x2  ", multi.Player2ComboRemaining);
                Render(player1LockText, "P1 LOCKED  ", multi.Player1LockRemaining);
                Render(player2LockText, "P2 LOCKED  ", multi.Player2LockRemaining);
            }
            if (feedbackText == null || feedbackLeft <= 0f) return;
            feedbackLeft -= Time.unscaledDeltaTime;
            if (feedbackLeft <= 0f) feedbackText.enabled = false;
        }

        private static void Render(TextMeshProUGUI label, string prefix, float seconds)
        {
            if (label == null) return;
            label.enabled = seconds > 0.01f;
            if (label.enabled) label.text = prefix + seconds.ToString("0.0") + "s";
        }
    }
}
