using DuckSoup.Features.Transitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.Menu.Views
{
    [DisallowMultipleComponent]
    public sealed class TableInformationPanel : MonoBehaviour
    {
        [Header("Buttons and panel")]
        [SerializeField] private Button informationButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject informationPanel;
        [SerializeField] private UIPanelTransition transition;

        [Header("Table selector")]
        [SerializeField] private TableSelectorView tableSelector;

        [Header("Panel texts")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        private bool closing;

        private void Awake()
        {
            if (informationButton != null)
                informationButton.onClick.AddListener(Open);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (informationPanel != null)
                informationPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (tableSelector != null)
                tableSelector.TableViewed += UpdateContent;
        }

        public void Open()
        {
            if (closing || informationPanel == null)
                return;

            TableDisplayData table = tableSelector != null
                ? tableSelector.CurrentTable
                : null;

            if (table == null)
            {
                Debug.LogWarning(
                    "[TableInformationPanel] Asigna Table Selector " +
                    "y abre el selector antes.",
                    this);

                return;
            }

            UpdateContent(table);

            if (transition != null)
                transition.Show();
            else
                informationPanel.SetActive(true);
        }

        private void UpdateContent(TableDisplayData table)
        {
            if (table == null)
                return;

            if (titleText != null)
                titleText.text = table.DisplayName;

            if (descriptionText != null)
                descriptionText.text = table.InformationText ?? string.Empty;
        }

        public void Close()
        {
            if (closing || informationPanel == null)
                return;

            if (transition != null)
            {
                closing = true;
                transition.Hide(() => closing = false);
            }
            else
            {
                informationPanel.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (tableSelector != null)
                tableSelector.TableViewed -= UpdateContent;

            closing = false;

            if (informationPanel != null)
                informationPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (informationButton != null)
                informationButton.onClick.RemoveListener(Open);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }
    }
}