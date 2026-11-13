
using DuckSoup.Domain;
using DuckSoup.Features.Round.Views;
using DuckSoup.Features.SinglePlayer.Services;
using DuckSoup.Features.TwoPlayer.Services;
using TMPro;
using UnityEngine;

namespace DuckSoup.Features.PowerUps
{
    public sealed class PowerUpHud : MonoBehaviour
    {
        [Header("Old text references - automatically hidden")]
        [SerializeField] private TextMeshProUGUI combo1Text;
        [SerializeField] private TextMeshProUGUI combo2Text;
        [SerializeField] private TextMeshProUGUI player1LockText;
        [SerializeField] private TextMeshProUGUI player2LockText;

        [Header("Existing power-up messages")]
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField, Min(0.1f)]
        private float feedbackSeconds = 1.2f;

        [Header("Recipe roots - assign in the Inspector")]
        [SerializeField] private Transform singlePlayerCardsRoot;
        [SerializeField] private Transform player1CardsRoot;
        [SerializeField] private Transform player2CardsRoot;

        private SinglePlayerSession single;
        private TwoPlayerSession multi;
        private float feedbackLeft;

        private int lastCountP1;
        private int lastCountP2;

        private RecipeCardView[] cards1;
        private RecipeCardView[] cards2;
        private bool warnedMissingRoot;

        public void Initialize(SinglePlayerSession session)
        {
            single = session;
            multi = null;
            lastCountP1 = 0;
            lastCountP2 = 0;
            cards1 = null;
            cards2 = null;
            HideOldText();
        }

        public void Initialize(TwoPlayerSession session)
        {
            multi = session;
            single = null;
            lastCountP1 = 0;
            lastCountP2 = 0;
            cards1 = null;
            cards2 = null;
            HideOldText();
        }

        public void ShowFeedback(string message)
        {
            if (feedbackText == null) return;

            feedbackText.text = message;
            feedbackText.enabled = true;
            feedbackLeft = feedbackSeconds;
        }

        private void HideOldText()
        {
            if (combo1Text != null) combo1Text.enabled = false;
            if (combo2Text != null) combo2Text.enabled = false;
            if (player1LockText != null) player1LockText.enabled = false;
            if (player2LockText != null) player2LockText.enabled = false;
        }

        private void Update()
        {
            if (single != null)
            {
                if (TryBind(ref cards1, singlePlayerCardsRoot))
                {
                    foreach (RecipeCardView card in cards1)
                    {
                        GetOverlay(card).Render(
                            single.IsCompletedCard(card.CardId),
                            single.ComboFill01);
                    }

                    int count = single.CurrentComboCount;

                    if (count > lastCountP1 && count >= 2)
                    {
                        PopFor(
                            cards1,
                            single.LastCompletedCardId,
                            count);
                    }

                    lastCountP1 = count;
                }
            }
            else if (multi != null)
            {
                if (TryBind(ref cards1, player1CardsRoot))
                {
                    UpdatePlayer(
                        cards1,
                        PlayerId.Player1,
                        ref lastCountP1);
                }

                if (TryBind(ref cards2, player2CardsRoot))
                {
                    UpdatePlayer(
                        cards2,
                        PlayerId.Player2,
                        ref lastCountP2);
                }
            }

            if (feedbackText != null && feedbackLeft > 0f)
            {
                feedbackLeft -= Time.unscaledDeltaTime;

                if (feedbackLeft <= 0f)
                {
                    feedbackText.enabled = false;
                }
            }
        }

        private void UpdatePlayer(
            RecipeCardView[] views,
            PlayerId player,
            ref int previousCount)
        {
            float fill = multi.GetComboFill01(player);

            foreach (RecipeCardView card in views)
            {
                GetOverlay(card).Render(
                    multi.IsCompletedCard(
                        player,
                        card.CardId),
                    fill);
            }

            int count = multi.GetComboCount(player);

            if (count > previousCount && count >= 2)
            {
                PopFor(
                    views,
                    multi.GetLastCompletedCardId(player),
                    count);
            }

            previousCount = count;
        }

        private bool TryBind(
            ref RecipeCardView[] views,
            Transform root)
        {
            if (views != null && views.Length == 3)
                return true;

            if (root == null)
            {
                if (!warnedMissingRoot)
                {
                    Debug.LogWarning(
                        "[PowerUpHud] Asigna los CardsRoot en el Inspector.",
                        this);

                    warnedMissingRoot = true;
                }

                return false;
            }

            RecipeCardView[] found =
                root.GetComponentsInChildren<RecipeCardView>(true);

            if (found.Length != 3)
                return false;

            views = found;

            foreach (RecipeCardView card in views)
            {
                GetOverlay(card);
            }

            return true;
        }

        private static RecipeComboOverlay GetOverlay(
            RecipeCardView card)
        {
            RecipeComboOverlay overlay =
                card.GetComponent<RecipeComboOverlay>();

            if (overlay == null)
            {
                overlay = card.gameObject.AddComponent<
                    RecipeComboOverlay>();
            }

            return overlay;
        }

        private static void PopFor(
            RecipeCardView[] cards,
            string completedId,
            int multiplier)
        {
            if (string.IsNullOrEmpty(completedId))
                return;

            foreach (RecipeCardView card in cards)
            {
                if (card.CardId != completedId)
                    continue;

                GetOverlay(card).PlayCombo(multiplier);
                break;
            }
        }
    }
}
