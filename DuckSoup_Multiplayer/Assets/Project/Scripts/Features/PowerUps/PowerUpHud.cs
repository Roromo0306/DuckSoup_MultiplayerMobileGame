using System.Collections.Generic;
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

        [Header("Power-up messages")]
        [SerializeField] private TextMeshProUGUI feedbackText;

        [SerializeField, Min(0.1f)]
        private float feedbackSeconds = 1.2f;

        [Header("Recipe roots")]
        [SerializeField] private Transform singlePlayerCardsRoot;
        [SerializeField] private Transform player1CardsRoot;
        [SerializeField] private Transform player2CardsRoot;

        private struct ComboPopup
        {
            public PlayerId Player;
            public int Slot;
            public int Multiplier;
        }

        private readonly Queue<ComboPopup> pending =
            new Queue<ComboPopup>();

        private SinglePlayerSession single;
        private TwoPlayerSession multi;

        private RecipeCardView[] cards1;
        private RecipeCardView[] cards2;

        private float feedbackLeft;

        public void Initialize(SinglePlayerSession session)
        {
            Unsubscribe();

            single = session;
            multi = null;

            cards1 = null;
            cards2 = null;
            pending.Clear();

            single.ComboScored += QueueSingleCombo;

            HideOldText();
        }

        public void Initialize(TwoPlayerSession session)
        {
            Unsubscribe();

            multi = session;
            single = null;

            cards1 = null;
            cards2 = null;
            pending.Clear();

            multi.ComboScored += QueueCombo;

            HideOldText();
        }

        private void QueueSingleCombo(
            int slot,
            int multiplier)
        {
            QueueCombo(PlayerId.Player1, slot, multiplier);
        }

        private void QueueCombo(
            PlayerId player,
            int slot,
            int multiplier)
        {
            pending.Enqueue(new ComboPopup
            {
                Player = player,
                Slot = slot,
                Multiplier = multiplier
            });
        }

        public void ShowFeedback(string message)
        {
            if (feedbackText == null)
                return;

            feedbackText.text = message;
            feedbackText.enabled = true;
            feedbackLeft = feedbackSeconds;
        }

        private void HideOldText()
        {
            if (combo1Text != null)
                combo1Text.enabled = false;

            if (combo2Text != null)
                combo2Text.enabled = false;

            if (player1LockText != null)
                player1LockText.enabled = false;

            if (player2LockText != null)
                player2LockText.enabled = false;
        }

        private void Update()
        {
            if (single != null &&
                TryBind(ref cards1, singlePlayerCardsRoot))
            {
                foreach (RecipeCardView card in cards1)
                {
                    GetOverlay(card).Render(
                        single.IsCompletedCard(card.CardId),
                        single.ComboFill01);
                }
            }

            if (multi != null)
            {
                RenderPlayer(
                    ref cards1,
                    player1CardsRoot,
                    PlayerId.Player1);

                RenderPlayer(
                    ref cards2,
                    player2CardsRoot,
                    PlayerId.Player2);
            }

            int count = pending.Count;

            for (int i = 0; i < count; i++)
            {
                ComboPopup next = pending.Dequeue();

                RecipeCardView[] cards =
                    next.Player == PlayerId.Player1
                        ? cards1
                        : cards2;

                if (cards == null)
                {
                    pending.Enqueue(next);
                    continue;
                }

                if (next.Slot >= 0 && next.Slot < cards.Length)
                {
                    GetOverlay(cards[next.Slot]).PlayCombo(
                        next.Multiplier);
                }
            }

            if (feedbackText != null && feedbackLeft > 0f)
            {
                feedbackLeft -= Time.deltaTime;

                if (feedbackLeft <= 0f)
                    feedbackText.enabled = false;
            }
        }

        private void RenderPlayer(
            ref RecipeCardView[] cards,
            Transform root,
            PlayerId player)
        {
            if (!TryBind(ref cards, root))
                return;

            foreach (RecipeCardView card in cards)
            {
                GetOverlay(card).Render(
                    multi.IsCompletedCard(player, card.CardId),
                    multi.GetComboFill01(player));
            }
        }

        private static bool TryBind(
            ref RecipeCardView[] cards,
            Transform root)
        {
            if (cards != null && cards.Length == 3)
                return true;

            if (root == null)
                return false;

            RecipeCardView[] found =
                root.GetComponentsInChildren<RecipeCardView>(true);

            if (found.Length != 3)
                return false;

            cards = found;
            return true;
        }

        private static RecipeComboOverlay GetOverlay(
            RecipeCardView card)
        {
            RecipeComboOverlay overlay =
                card.GetComponent<RecipeComboOverlay>();

            return overlay != null
                ? overlay
                : card.gameObject.AddComponent<RecipeComboOverlay>();
        }

        private void Unsubscribe()
        {
            if (single != null)
                single.ComboScored -= QueueSingleCombo;

            if (multi != null)
                multi.ComboScored -= QueueCombo;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}