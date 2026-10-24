
using System;
using System.Collections.Generic;
using DuckSoup.Features.Menu.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.Menu.Views
{
    /// <summary>
    /// Datos visuales de una mesa seleccionable.
    /// </summary>
    [Serializable]
    public sealed class TableDisplayData
    {
        [SerializeField]
        private string tableId = "classic_table";

        [SerializeField]
        private string displayName = "Classic Table";

        [SerializeField]
        private Sprite previewSprite;

        public string Id => tableId;

        public string DisplayName => displayName;

        public Sprite PreviewSprite => previewSprite;
    }

    /// <summary>
    /// Vista de selección de mesas.
    ///
    /// Muestra imagen, nombre, récord
    /// y controla el botón Hold to Play.
    /// </summary>
    public sealed class TableSelectorView : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField]
        private GameObject panelRoot;

        [Header("Table Information")]
        [SerializeField]
        private Image tableImage;

        [SerializeField]
        private TextMeshProUGUI tableNameText;

        [SerializeField]
        private TextMeshProUGUI highScoreText;

        [Header("Navigation")]
        [SerializeField]
        private Button previousButton;

        [SerializeField]
        private Button nextButton;

        [SerializeField]
        private Button closeButton;

        [Header("Hold Action")]
        [SerializeField]
        private HoldToPlayButton holdToPlayButton;

        [Header("Available Tables")]
        [SerializeField]
        private TableDisplayData[] tables =
            new TableDisplayData[0];

        private IPlayerProgressStore progressStore;
        private Action<TableDisplayData> tableConfirmed;

        private int selectedIndex;
        private bool initialized;

        public void Initialize(
            IPlayerProgressStore store,
            Action<TableDisplayData> onTableConfirmed)
        {
            if (initialized)
                return;

            progressStore = store ??
                throw new ArgumentNullException(nameof(store));

            tableConfirmed = onTableConfirmed ??
                throw new ArgumentNullException(
                    nameof(onTableConfirmed));

            ValidateReferences();
            ValidateTables();

            previousButton.onClick.AddListener(
                PreviousTable);

            nextButton.onClick.AddListener(
                NextTable);

            closeButton.onClick.AddListener(
                Hide);

            holdToPlayButton.Completed +=
                HandleHoldCompleted;

            selectedIndex = 0;
            initialized = true;

            panelRoot.SetActive(false);
        }

        public void Open()
        {
            if (!initialized)
            {
                Debug.LogError(
                    "[TableSelector] No está inicializado.",
                    this);

                return;
            }

            selectedIndex = 0;

            panelRoot.SetActive(true);

            Refresh();
        }

        public void Hide()
        {
            holdToPlayButton.ResetHold();

            panelRoot.SetActive(false);
        }

        private void NextTable()
        {
            selectedIndex =
                (selectedIndex + 1) % tables.Length;

            Refresh();
        }

        private void PreviousTable()
        {
            selectedIndex =
                (selectedIndex - 1 + tables.Length) %
                tables.Length;

            Refresh();
        }

        private void Refresh()
        {
            TableDisplayData table = tables[selectedIndex];

            tableNameText.text = table.DisplayName;

            int highScore =
                progressStore.GetTableHighScore(table.Id);

            highScoreText.text =
                $"HIGH SCORE: {highScore} RECIPES";

            tableImage.sprite = table.PreviewSprite;

            tableImage.enabled =
                table.PreviewSprite != null;

            bool multipleTables = tables.Length > 1;

            previousButton.interactable = multipleTables;
            nextButton.interactable = multipleTables;

            holdToPlayButton.ResetHold();
        }

        private void HandleHoldCompleted()
        {
            TableDisplayData selectedTable =
                tables[selectedIndex];

            Hide();

            tableConfirmed?.Invoke(selectedTable);
        }

        private void ValidateReferences()
        {
            if (panelRoot == null ||
                tableImage == null ||
                tableNameText == null ||
                highScoreText == null ||
                previousButton == null ||
                nextButton == null ||
                closeButton == null ||
                holdToPlayButton == null)
            {
                throw new InvalidOperationException(
                    "[TableSelector] Faltan referencias " +
                    "en el Inspector.");
            }
        }

        private void ValidateTables()
        {
            if (tables == null || tables.Length == 0)
            {
                throw new InvalidOperationException(
                    "[TableSelector] Añade al menos una mesa.");
            }

            var ids = new HashSet<string>(
                StringComparer.Ordinal);

            foreach (TableDisplayData table in tables)
            {
                if (table == null ||
                    string.IsNullOrWhiteSpace(table.Id) ||
                    string.IsNullOrWhiteSpace(
                        table.DisplayName))
                {
                    throw new InvalidOperationException(
                        "[TableSelector] Hay una mesa " +
                        "con datos incompletos.");
                }

                if (!ids.Add(table.Id.Trim()))
                {
                    throw new InvalidOperationException(
                        "[TableSelector] Hay IDs de mesa " +
                        "duplicados.");
                }
            }
        }

        private void OnDestroy()
        {
            if (!initialized)
                return;

            previousButton.onClick.RemoveListener(
                PreviousTable);

            nextButton.onClick.RemoveListener(
                NextTable);

            closeButton.onClick.RemoveListener(
                Hide);

            if (holdToPlayButton != null)
            {
                holdToPlayButton.Completed -=
                    HandleHoldCompleted;
            }
        }
    }
}
