using System;
using System.Collections.Generic;
using DuckSoup.Domain;

namespace DuckSoup.Features.Round.Models
{
    public sealed class RoundModel
    {
        private readonly IReadOnlyList<IngredientToken> tokens;

        private readonly Dictionary<string, IngredientToken>tokensById;

        public string Id { get; }
        public RoundPlayerModel Player1 { get; }
        public RoundPlayerModel Player2 { get; }
        public IReadOnlyList<IngredientToken> Tokens => tokens;
        public int TotalTokenCount => tokens.Count;

        public int ConsumedTokenCount
        {
            get
            {
                int consumed = 0;

                foreach (IngredientToken token in tokens)
                {
                    if (token.IsConsumed)
                    {
                        consumed++;
                    }
                }

                return consumed;
            }
        }

        public RoundModel(
            string id,
            RoundPlayerModel player1,
            RoundPlayerModel player2,
            IEnumerable<IngredientToken> tableTokens)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("La ronda necesita un ID válido.",nameof(id));
            }

            Player1 = player1 ?? throw new ArgumentNullException(nameof(player1));

            Player2 = player2 ?? throw new ArgumentNullException(nameof(player2));

            if (Player1.Id != PlayerId.Player1 || Player2.Id != PlayerId.Player2)
            {
                throw new ArgumentException("Los jugadores están mal asignados.");
            }

            if (Player1.Cards.Count != Player2.Cards.Count)
            {
                throw new ArgumentException("Ambos jugadores deben tener " +"la misma cantidad de cartas.");
            }

            if (tableTokens == null)
            {
                throw new ArgumentNullException(nameof(tableTokens));
            }

            var tokenList = new List<IngredientToken>();

            tokensById = new Dictionary<string, IngredientToken>(StringComparer.Ordinal);

            foreach (IngredientToken token in tableTokens)
            {
                if (token == null)
                {
                    throw new ArgumentException("La mesa contiene un token nulo.",nameof(tableTokens));
                }

                if (tokensById.ContainsKey(token.Id))
                {
                    throw new ArgumentException($"Token duplicado: {token.Id}",nameof(tableTokens));
                }

                tokenList.Add(token);
                tokensById.Add(token.Id, token);
            }

            if (tokenList.Count == 0)
            {
                throw new ArgumentException("La mesa necesita ingredientes.",nameof(tableTokens));
            }

            ValidateCardIds(Player1, Player2);

            ValidateIngredientSupply(Player1, Player2, tokenList);

            Id = id.Trim();

            tokens = tokenList.AsReadOnly();
        }

        public bool TryGetPlayer(PlayerId playerId,out RoundPlayerModel player)
        {
            switch (playerId)
            {
                case PlayerId.Player1:
                    player = Player1;
                    return true;

                case PlayerId.Player2:
                    player = Player2;
                    return true;

                default:
                    player = null;
                    return false;
            }
        }

  
        public bool TryGetCard(PlayerId playerId,string cardId,out RecipeCardModel card)
        {
            card = null;

            if (!TryGetPlayer(playerId, out RoundPlayerModel player))
            {
                return false;
            }

            return player.TryGetCard(cardId, out card);
        }

        public bool TryGetToken(string tokenId,out IngredientToken token)
        {
            token = null;

            if (string.IsNullOrWhiteSpace(tokenId))
                return false;

            return tokensById.TryGetValue(tokenId, out token);
        }

        private static void ValidateCardIds(RoundPlayerModel player1,RoundPlayerModel player2)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);

            AddCardIds(player1, ids);
            AddCardIds(player2, ids);
        }

        private static void AddCardIds(RoundPlayerModel player,HashSet<string> ids)
        {
            foreach (RecipeCardModel card in player.Cards)
            {
                if (!ids.Add(card.CardId))
                {
                    throw new ArgumentException($"ID de carta duplicado: {card.CardId}");
                }
            }
        }
        private static void ValidateIngredientSupply(
            RoundPlayerModel player1,
            RoundPlayerModel player2,
            IReadOnlyList<IngredientToken> tableTokens)
        {
            var required = new Dictionary<IngredientType, int>();

            AddRequiredIngredients(player1, required);
            AddRequiredIngredients(player2, required);

            var available = new Dictionary<IngredientType, int>();

            foreach (IngredientToken token in tableTokens)
            {
                available.TryGetValue(token.Ingredient, out int count);

                available[token.Ingredient] = count + 1;
            }

            foreach (KeyValuePair<IngredientType, int> entry in required)
            {
                available.TryGetValue(entry.Key, out int availableCount);

                if (availableCount < entry.Value)
                {
                    throw new ArgumentException(
                        $"Faltan ingredientes {entry.Key}. " +
                        $"Necesarios: {entry.Value}. " +
                        $"Disponibles: {availableCount}.",
                        nameof(tableTokens));
                }
            }
        }

        private static void AddRequiredIngredients(RoundPlayerModel player,Dictionary<IngredientType, int> required)
        {
            foreach (RecipeCardModel card in player.Cards)
            {
                foreach (IngredientType ingredient in card.Definition.RequiredIngredients)
                {
                    required.TryGetValue(ingredient, out int count);

                    required[ingredient] = count + 1;
                }
            }
        }
    }
}
