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

        [TextArea(4, 12)]
        [SerializeField] private string informationText = "";

        [Header("Medal scores")]
        [SerializeField, Min(1)] private int bronzeScore = 10;
        [SerializeField, Min(1)] private int silverScore = 20;
        [SerializeField, Min(1)] private int goldScore = 30;
        [SerializeField, Min(1)] private int masterScore = 40;

        public string Id => tableId;
        public string DisplayName => displayName;
        public Sprite PreviewSprite => previewSprite;
        public string InformationText => informationText;

        public int BronzeScore => bronzeScore;
        public int SilverScore => silverScore;
        public int GoldScore => goldScore;
        public int MasterScore => masterScore;

        public bool HasValidMedalScores =>
            bronzeScore > 0 &&
            silverScore > bronzeScore &&
            goldScore > silverScore &&
            masterScore > goldScore;
    }

    public sealed class TableSelectorView : MonoBehaviour
    {
        [Header("Popup")]
        [SerializeField] private GameObject panelRoot;

        [Header("Table Information")]
        [SerializeField] private Image tableImage;
        [SerializeField] private TextMeshProUGUI tableNameText;
        [SerializeField] private TextMeshProUGUI highScoreText;

        [Header("Medals")]
        [SerializeField] private TableMedalsView medalsView;

        [Header("Navigation")]
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button closeButton;

        [Header("SELECT Button Inside Popup")]
        [SerializeField] private Button playButton;
        [SerializeField] private TextMeshProUGUI selectButtonText;

        [Header("Button Text Outside Popup")]
        [SerializeField] private TextMeshProUGUI mainMenuSelectedTableText;

        [Header("Tables")]
        [SerializeField]
        private TableDisplayData[] tables =
            new TableDisplayData[0];

        [Header("Game Feel")]
        [SerializeField] private UICarouselFeel carouselFeel;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private IPlayerProgressStore progressStore;
        private Action<TableDisplayData> tableConfirmed;

        private int selectedIndex;
        private bool initialized;
        private bool confirming;

        public event Action<TableDisplayData> TableViewed;

        public TableDisplayData CurrentTable
        {
            get
            {
                if (!initialized ||
                    tables == null ||
                    selectedIndex < 0 ||
                    selectedIndex >= tables.Length)
                {
                    return null;
                }

                return tables[selectedIndex];
            }
        }

        public void Initialize(
            IPlayerProgressStore store,
            Action<TableDisplayData> onTableConfirmed)
        {
            if (initialized)
                return;

            progressStore = store ??
                throw new ArgumentNullException(nameof(store));

            tableConfirmed = onTableConfirmed ??
                throw new ArgumentNullException(nameof(onTableConfirmed));

            ValidateReferences();
            ValidateTables();

            previousButton.onClick.AddListener(PreviousTable);
            nextButton.onClick.AddListener(NextTable);
            closeButton.onClick.AddListener(Hide);

            if (selectButtonText == null)
            {
                selectButtonText =
                    playButton.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (selectButtonText != null)
                selectButtonText.text = "SELECT";

            selectedIndex = 0;
            initialized = true;

            panelRoot.SetActive(false);

            Log("Initialized.");
        }

        public void Open()
        {
            if (!initialized)
                return;

            confirming = false;
            panelRoot.SetActive(true);

            Refresh();

            carouselFeel?.ResetVisual(true);
        }

        public void Hide()
        {
            if (panelRoot == null)
                return;

            panelRoot.SetActive(false);
        }

        private void PreviousTable()
        {
            Move(-1);
        }

        private void NextTable()
        {
            Move(1);
        }

        private void Move(int direction)
        {
            if (!initialized || confirming || tables.Length < 2)
                return;

            if (carouselFeel != null && carouselFeel.IsSwitching)
                return;

            if (carouselFeel == null)
            {
                ApplyMove(direction);
            }
            else
            {
                carouselFeel.Switch(
                    direction,
                    () => ApplyMove(direction));
            }
        }

        private void ApplyMove(int direction)
        {
            selectedIndex =
                (selectedIndex + direction + tables.Length) % tables.Length;

            Refresh();
        }

        private void Refresh()
        {
            if (!initialized)
                return;

            TableDisplayData table = tables[selectedIndex];

            tableNameText.text = table.DisplayName;

            int highScore =
                progressStore.GetTableHighScore(table.Id);

            highScoreText.text = "HIGH SCORE: " + highScore;

            if (medalsView != null)
                medalsView.Render(table, highScore);

            tableImage.sprite = table.PreviewSprite;
            tableImage.enabled = table.PreviewSprite != null;

            previousButton.interactable = tables.Length > 1;
            nextButton.interactable = tables.Length > 1;

            if (selectButtonText != null)
                selectButtonText.text = "SELECT";

            TableViewed?.Invoke(table);
        }

        public void ConfirmSelectionFromDirectInput()
        {
            if (!initialized ||
                confirming ||
                tables == null ||
                tables.Length == 0)
            {
                return;
            }

            confirming = true;

            TableDisplayData selected = tables[selectedIndex];

            try
            {
                if (mainMenuSelectedTableText != null)
                {
                    mainMenuSelectedTableText.text =
                        selected.DisplayName;
                }

                tableConfirmed?.Invoke(selected);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
            finally
            {
                Hide();
                confirming = false;
            }
        }

        public bool TryGetTableById(
            string tableId,
            out TableDisplayData table)
        {
            table = null;

            if (tables == null ||
                string.IsNullOrWhiteSpace(tableId))
            {
                return false;
            }

            string id = tableId.Trim();

            foreach (TableDisplayData candidate in tables)
            {
                if (candidate != null &&
                    !string.IsNullOrWhiteSpace(candidate.Id) &&
                    string.Equals(
                        candidate.Id.Trim(),
                        id,
                        StringComparison.Ordinal))
                {
                    table = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TrySelectTableById(
            string tableId,
            out TableDisplayData selected)
        {
            selected = null;

            if (!initialized || string.IsNullOrWhiteSpace(tableId))
                return false;

            for (int i = 0; i < tables.Length; i++)
            {
                if (!string.Equals(
                    tables[i].Id,
                    tableId,
                    StringComparison.Ordinal))
                {
                    continue;
                }

                selectedIndex = i;
                selected = tables[i];

                if (panelRoot.activeSelf)
                    Refresh();

                return true;
            }

            return false;
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
                playButton == null)
            {
                throw new InvalidOperationException(
                    "[TableSelector] Missing Inspector references.");
            }
        }

        private void ValidateTables()
        {
            if (tables == null || tables.Length == 0)
            {
                throw new InvalidOperationException(
                    "[TableSelector] No tables configured.");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);

            foreach (TableDisplayData table in tables)
            {
                if (table == null ||
                    string.IsNullOrWhiteSpace(table.Id) ||
                    string.IsNullOrWhiteSpace(table.DisplayName) ||
                    !ids.Add(table.Id.Trim()))
                {
                    throw new InvalidOperationException(
                        "[TableSelector] Invalid table data.");
                }

                if (!table.HasValidMedalScores)
                {
                    throw new InvalidOperationException(
                        "[TableSelector] Revisa las puntuaciones de " +
                        table.DisplayName +
                        ": 0 < Bronze < Silver < Gold < Master.");
                }
            }
        }

        private void Log(string message)
        {
            if (showDebugLogs)
                Debug.Log("[TableSelector] " + message, this);
        }

        private void OnDestroy()
        {
            if (!initialized)
                return;

            previousButton.onClick.RemoveListener(PreviousTable);
            nextButton.onClick.RemoveListener(NextTable);
            closeButton.onClick.RemoveListener(Hide);
        }
    }
}