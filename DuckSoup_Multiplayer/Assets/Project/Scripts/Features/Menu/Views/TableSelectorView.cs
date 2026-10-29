using System;
using System.Collections.Generic;
using DuckSoup.Features.GameFeel.UI;
using DuckSoup.Features.Menu.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.Menu.Views
{
    [Serializable]
    public sealed class TableDisplayData
    {
        [SerializeField] private string tableId = "classic_table";
        [SerializeField] private string displayName = "Classic Table";
        [SerializeField] private Sprite previewSprite;
        public string Id => tableId;
        public string DisplayName => displayName;
        public Sprite PreviewSprite => previewSprite;
    }

    public sealed class TableSelectorView : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [Header("Table information")]
        [SerializeField] private Image tableImage;
        [SerializeField] private TextMeshProUGUI tableNameText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [Header("Navigation")]
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button closeButton;
        [Header("Hold action")]
        [SerializeField] private HoldToPlayButton holdToPlayButton;
        [Header("Tables")]
        [SerializeField] private TableDisplayData[] tables = new TableDisplayData[0];
        [Header("Game Feel - visual group of image, title and highscore")]
        [SerializeField] private UICarouselFeel carouselFeel;

        private IPlayerProgressStore progressStore;
        private Action<TableDisplayData> tableConfirmed;
        private int selectedIndex;
        private bool initialized;

        public void Initialize(
            IPlayerProgressStore store, Action<TableDisplayData> onTableConfirmed)
        {
            if (initialized) return;
            progressStore = store ?? throw new ArgumentNullException(nameof(store));
            tableConfirmed = onTableConfirmed ??
                throw new ArgumentNullException(nameof(onTableConfirmed));
            ValidateReferences();
            ValidateTables();
            previousButton.onClick.AddListener(PreviousTable);
            nextButton.onClick.AddListener(NextTable);
            closeButton.onClick.AddListener(Hide);
            holdToPlayButton.Completed += HandleHoldCompleted;
            selectedIndex = 0;
            initialized = true;
            panelRoot.SetActive(false);
        }

        public void Open()
        {
            if (!initialized)
            {
                Debug.LogError("[TableSelector] No inicializado.", this);
                return;
            }
            selectedIndex = 0;
            panelRoot.SetActive(true);
            Refresh();
            carouselFeel?.ResetVisual(true); // La mesa visible queda ligeramente ampliada.
        }

        public void Hide()
        {
            holdToPlayButton.ResetHold();
            panelRoot.SetActive(false);
        }

        private void NextTable() => Move(1);
        private void PreviousTable() => Move(-1);

        private void Move(int direction)
        {
            if (tables.Length < 2 ||
                (carouselFeel != null && carouselFeel.IsSwitching)) return;
            holdToPlayButton.ResetHold();
            if (carouselFeel == null)
                ApplyMove(direction);
            else
                carouselFeel.Switch(direction, () => ApplyMove(direction));
        }

        private void ApplyMove(int direction)
        {
            selectedIndex = (selectedIndex + direction + tables.Length) % tables.Length;
            Refresh();
        }

        private void Refresh()
        {
            TableDisplayData table = tables[selectedIndex];
            tableNameText.text = table.DisplayName;
            highScoreText.text =
                $"HIGH SCORE: {progressStore.GetTableHighScore(table.Id)} RECIPES";
            tableImage.sprite = table.PreviewSprite;
            tableImage.enabled = table.PreviewSprite != null;
            bool multiple = tables.Length > 1;
            previousButton.interactable = multiple;
            nextButton.interactable = multiple;
            holdToPlayButton.ResetHold();
        }

        private void HandleHoldCompleted()
        {
            // Evitar confirmar una mesa mientras la imagen esta cambiando.
            if (carouselFeel != null && carouselFeel.IsSwitching) return;
            TableDisplayData selected = tables[selectedIndex];
            Hide();
            tableConfirmed?.Invoke(selected);
        }

        private void ValidateReferences()
        {
            if (panelRoot == null || tableImage == null || tableNameText == null ||
                highScoreText == null || previousButton == null || nextButton == null ||
                closeButton == null || holdToPlayButton == null)
                throw new InvalidOperationException(
                    "[TableSelector] Faltan referencias en el Inspector.");
        }

        private void ValidateTables()
        {
            if (tables == null || tables.Length == 0)
                throw new InvalidOperationException("[TableSelector] Anade una mesa.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (TableDisplayData table in tables)
            {
                if (table == null || string.IsNullOrWhiteSpace(table.Id) ||
                    string.IsNullOrWhiteSpace(table.DisplayName) ||
                    !ids.Add(table.Id.Trim()))
                    throw new InvalidOperationException("[TableSelector] Mesa invalida o ID duplicado.");
            }
        }

        private void OnDestroy()
        {
            if (!initialized) return;
            previousButton.onClick.RemoveListener(PreviousTable);
            nextButton.onClick.RemoveListener(NextTable);
            closeButton.onClick.RemoveListener(Hide);
            holdToPlayButton.Completed -= HandleHoldCompleted;
        }
    }
}
