
using DuckSoup.Domain;
using DuckSoup.Features.Match.Contracts;
using DuckSoup.Features.Match.Models;
using System;

using TMPro;
using UnityEngine;
using UnityEngine.UI;
// Evita conflictos con otras clases llamadas Image.
using Image = UnityEngine.UI.Image;

namespace DuckSoup.Features.Match.Views
{
    /// <summary>
    /// Vista encargada de mostrar los resultados
    /// de cada ronda y el resultado final de partida.
    ///
    /// Muestra el dinero conseguido y acumulado,
    /// el nivel de cada restaurante y su sprite.
    ///
    /// No contiene lógica económica ni modifica
    /// el progreso de los jugadores.
    /// </summary>
    public sealed class ResultsScreenView :
        MonoBehaviour, IMatchResultsView
    {
        [Header("Panel")]
        [SerializeField]
        private GameObject resultsPanel;

        [Header("Texts")]
        [SerializeField]
        private TextMeshProUGUI headlineText;

        [SerializeField]
        private TextMeshProUGUI player1Text;

        [SerializeField]
        private TextMeshProUGUI player2Text;

        [Header("Restaurant Images")]
        [SerializeField]
        private Image player1Building;

        [SerializeField]
        private Image player2Building;

        [Header("Restaurant Upgrade Sprites")]
        [Tooltip(
            "Sprites ordenados por nivel. " +
            "Element 0 = nivel inicial, " +
            "Element 4 = restaurante final.")]
        [SerializeField]
        private Sprite[] buildingStages = new Sprite[5];

        [Header("Continue Button")]
        [SerializeField]
        private Button continueButton;

        [SerializeField]
        private TextMeshProUGUI continueLabel;

        // Evento al que se suscribe CompositionRoot.
        public event Action ContinueRequested;

        private bool actionUsed;

        // Conservamos los sprites iniciales como respaldo.
        private Sprite player1DefaultSprite;
        private Sprite player2DefaultSprite;

        private void Awake()
        {
            // Guardamos las imágenes originales.
            if (player1Building != null)
            {
                player1DefaultSprite =
                    player1Building.sprite;
            }

            if (player2Building != null)
            {
                player2DefaultSprite =
                    player2Building.sprite;
            }

            // Registramos el botón una sola vez.
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(
                    HandleContinueClicked);
            }

            Hide();
        }

        /// <summary>
        /// Muestra los resultados de una ronda.
        /// </summary>
        public void ShowRound(
            RoundSettlementResult result)
        {
            Show(result, false);
        }

        /// <summary>
        /// Muestra el resultado final
        /// cuando termina la partida.
        /// </summary>
        public void ShowFinal(
            RoundSettlementResult result)
        {
            Show(result, true);
        }

        /// <summary>
        /// Prepara la pantalla según
        /// el tipo de resultado.
        /// </summary>
        private void Show(
            RoundSettlementResult result,
            bool isFinal)
        {
            if (result == null)
            {
                throw new ArgumentNullException(
                    nameof(result));
            }

            ValidateReferences();

            // Configuramos el encabezado y el botón.
            if (isFinal)
            {
                if (result.MatchWinner == PlayerId.None)
                {
                    headlineText.text =
                        "¡EMPATE DE PARTIDA!";
                }
                else
                {
                    headlineText.text =
                        "¡GANA LA PARTIDA!\n" +
                        GetPlayerName(result.MatchWinner);
                }

                continueLabel.text = "REVANCHA";
            }
            else
            {
                headlineText.text =
                    $"RONDA {result.RoundNumber}\n" +
                    "GANA " +
                    GetPlayerName(result.RoundWinner);

                continueLabel.text = "SIGUIENTE RONDA";
            }

            // Actualizamos independientemente
            // los resultados de ambos jugadores.
            RenderPlayer(
                player1Text,
                player1Building,
                result.Player1,
                result.MaxUpgradeLevel,
                player1DefaultSprite);

            RenderPlayer(
                player2Text,
                player2Building,
                result.Player2,
                result.MaxUpgradeLevel,
                player2DefaultSprite);

            // Permitimos una nueva pulsación
            // al mostrar otros resultados.
            actionUsed = false;
            continueButton.interactable = true;

            // Mostramos el panel completo.
            resultsPanel.SetActive(true);
        }

        /// <summary>
        /// Representa el dinero y el progreso
        /// del restaurante de un jugador.
        /// </summary>
        private void RenderPlayer(
            TextMeshProUGUI label,
            Image building,
            PlayerEconomySnapshot player,
            int maxLevel,
            Sprite defaultSprite)
        {
            // Texto de la próxima mejora.
            string nextUpgrade;

            if (player.NextUpgradeAt == 0)
            {
                nextUpgrade =
                    "RESTAURANTE AL MÁXIMO";
            }
            else
            {
                nextUpgrade =
                    $"Próxima mejora: " +
                    $"{player.NextUpgradeAt} monedas";
            }

            // Actualizamos los textos.
            label.text =
                $"{GetPlayerName(player.Id)}\n" +
                $"+{player.EarnedThisRound} monedas\n" +
                $"Total: {player.TotalCoins} monedas\n" +
                $"Restaurante: " +
                $"{player.UpgradeLevel}/{maxLevel}\n" +
                nextUpgrade;

            // Actualizamos la imagen del restaurante.
            UpdateRestaurantSprite(
                building,
                player.UpgradeLevel,
                maxLevel,
                defaultSprite);
        }

        /// <summary>
        /// Cambia el sprite del restaurante
        /// según su nivel de mejora.
        ///
        /// Ambos jugadores comparten el catálogo
        /// de sprites, pero pueden mostrar niveles
        /// diferentes simultáneamente.
        /// </summary>
        private void UpdateRestaurantSprite(
            Image building,
            int level,
            int maxLevel,
            Sprite defaultSprite)
        {
            if (building == null)
                return;

            bool validLevel =
                level >= 0 &&
                level <= maxLevel;

            bool validArray =
                buildingStages != null &&
                buildingStages.Length == maxLevel + 1;

            bool validSprite =
                validLevel &&
                validArray &&
                buildingStages[level] != null;

            if (validSprite)
            {
                // Seleccionamos la imagen del nivel.
                building.sprite = buildingStages[level];
            }
            else
            {
                // Si falta configuración, mostramos
                // el sprite inicial como respaldo.
                building.sprite = defaultSprite;

                Debug.LogWarning(
                    $"[ResultsScreen] No hay sprite " +
                    $"válido para el nivel {level}. " +
                    $"Comprueba Building Stages.",
                    this);
            }

            // Mantener los colores reales del sprite.
            // No utilizamos Color.Lerp.
            building.color = Color.white;

            // Aseguramos que la imagen sea visible.
            building.enabled = true;
            building.type = Image.Type.Simple;
            building.preserveAspect = true;
        }

        /// <summary>
        /// Devuelve el nombre visual de un jugador.
        /// </summary>
        private static string GetPlayerName(
            PlayerId id)
        {
            switch (id)
            {
                case PlayerId.Player1:
                    return "JUGADOR 1";

                case PlayerId.Player2:
                    return "JUGADOR 2";

                default:
                    return "NINGUNO";
            }
        }

        /// <summary>
        /// Gestiona la pulsación del botón.
        ///
        /// Evita solicitar dos veces el avance
        /// si el usuario pulsa repetidamente.
        /// </summary>
        private void HandleContinueClicked()
        {
            if (actionUsed)
                return;

            actionUsed = true;

            if (continueButton != null)
            {
                continueButton.interactable = false;
            }

            ContinueRequested?.Invoke();
        }

        /// <summary>
        /// Oculta la pantalla de resultados.
        /// </summary>
        public void Hide()
        {
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Comprueba las referencias obligatorias.
        /// </summary>
        private void ValidateReferences()
        {
            if (resultsPanel == null ||
                headlineText == null ||
                player1Text == null ||
                player2Text == null ||
                player1Building == null ||
                player2Building == null ||
                continueButton == null ||
                continueLabel == null)
            {
                throw new InvalidOperationException(
                    "[ResultsScreen] Faltan referencias " +
                    "en el Inspector.");
            }
        }

        /// <summary>
        /// Eliminamos las suscripciones
        /// cuando se destruye la vista.
        /// </summary>
        private void OnDestroy()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(
                    HandleContinueClicked);
            }
        }
    }
}
