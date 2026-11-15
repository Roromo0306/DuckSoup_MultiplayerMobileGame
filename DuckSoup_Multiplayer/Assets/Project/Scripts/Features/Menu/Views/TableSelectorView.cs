
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
        [Header("Popup")]
        [SerializeField] private GameObject panelRoot;

        [Header("Table Information")]
        [SerializeField] private Image tableImage;
        [SerializeField] private TextMeshProUGUI tableNameText;
        [SerializeField] private TextMeshProUGUI highScoreText;

        [Header("Navigation")]
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button closeButton;

        [Header("SELECT Button Inside Popup")]
        [SerializeField] private Button playButton;
        [SerializeField] private TextMeshProUGUI selectButtonText;

        [Header("NEW - Button Text Outside Popup")]
        [Tooltip("El TMP del boton situado debajo de PLAY en el menu.")]
        [SerializeField]
        private TextMeshProUGUI mainMenuSelectedTableText;

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

            previousButton.onClick.AddListener(PreviousTable);
            nextButton.onClick.AddListener(NextTable);
            closeButton.onClick.AddListener(Hide);

            // IMPORTANTE:
            // No registramos OnClick para SELECT.
            // Lo controla TableSelectDirectInput.
            if (selectButtonText == null)
            {
                selectButtonText =
                    playButton.GetComponentInChildren<
                        TextMeshProUGUI>(true);
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

            Log("OPEN");
        }

        public void Hide()
        {
            if (panelRoot == null)
                return;

            panelRoot.SetActive(false);

            Log("CLOSE | Panel active: " +
                panelRoot.activeSelf);
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
            if (!initialized ||
                confirming ||
                tables.Length < 2)
                return;

            if (carouselFeel != null &&
                carouselFeel.IsSwitching)
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
                (selectedIndex + direction + tables.Length)
                % tables.Length;

            Refresh();
        }

        private void Refresh()
        {
            if (!initialized)
                return;

            TableDisplayData table =
                tables[selectedIndex];

            tableNameText.text = table.DisplayName;

            highScoreText.text =
                "HIGH SCORE: " +
                progressStore.GetTableHighScore(table.Id);

            tableImage.sprite = table.PreviewSprite;
            tableImage.enabled =
                table.PreviewSprite != null;

            previousButton.interactable =
                tables.Length > 1;

            nextButton.interactable =
                tables.Length > 1;

            if (selectButtonText != null)
                selectButtonText.text = "SELECT";
        }

        // NUEVO METODO PUBLICO.
        // Lo llama directamente el script del boton.
        public void ConfirmSelectionFromDirectInput()
        {
            Log("DIRECT SELECT RECEIVED");

            if (!initialized || confirming)
                return;

            if (tables == null || tables.Length == 0)
                return;

            confirming = true;

            TableDisplayData selected =
                tables[selectedIndex];

            try
            {
                // 1. Actualizar directamente el texto
                // del boton situado debajo de PLAY.
                if (mainMenuSelectedTableText != null)
                {
                    mainMenuSelectedTableText.text =
                        selected.DisplayName;
                }
                else
                {
                    Debug.LogWarning(
                        "[TableSelector] Asigna " +
                        "Main Menu Selected Table Text.",
                        this);
                }

                // 2. Avisar al Bootstrap.
                // Esto es importante: no basta con
                // cambiar el texto visual. Tambien
                // debe guardarse el TableId real.
                tableConfirmed?.Invoke(selected);

                Log("SELECTED: " + selected.Id);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
            finally
            {
                // 3. Cerrar SIEMPRE el popup.
                Hide();
                confirming = false;
            }
        }

        public bool TrySelectTableById(
            string tableId,
            out TableDisplayData selected)
        {
            selected = null;

            if (!initialized ||
                string.IsNullOrWhiteSpace(tableId))
                return false;

            for (int i = 0; i < tables.Length; i++)
            {
                if (!string.Equals(
                    tables[i].Id,
                    tableId,
                    StringComparison.Ordinal))
                    continue;

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

            var ids = new HashSet<string>(
                StringComparer.Ordinal);

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
            }
        }

        private void Log(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    "[TableSelector] " + message,
                    this);
            }
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
