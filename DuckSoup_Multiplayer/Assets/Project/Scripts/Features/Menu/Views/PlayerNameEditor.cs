using System.Collections;
using DuckSoup.Features.Menu.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.Menu.Views
{
    [DisallowMultipleComponent]
    public sealed class PlayerNameEditor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button changeNameButton;
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TMP_InputField nameInput;

        [Header("Name")]
        [SerializeField, Min(1)]
        private int maximumCharacters = 24;

        private bool prepared;
        private bool editing;

        private string originalName;
        private Coroutine focusRoutine;

        private void Awake()
        {
            if (changeNameButton == null ||
                playerNameText == null ||
                nameInput == null)
            {
                Debug.LogError(
                    "[PlayerNameEditor] Asigna Change Name Button, " +
                    "Player Name Text y Name Input.",
                    this);

                enabled = false;
                return;
            }

            if (nameInput.textComponent == null)
            {
                Debug.LogError(
                    "[PlayerNameEditor] Name Input necesita " +
                    "su Text Component: Text Area > Text.",
                    this);

                enabled = false;
                return;
            }

            if (transform.IsChildOf(nameInput.transform))
            {
                Debug.LogError(
                    "[PlayerNameEditor] Coloca el script en " +
                    "SinglePlayerStats, fuera de Name Input.",
                    this);

                enabled = false;
                return;
            }

            playerNameText.richText = false;

            nameInput.contentType =
                TMP_InputField.ContentType.Standard;

            nameInput.lineType =
                TMP_InputField.LineType.SingleLine;

            nameInput.keyboardType =
                TouchScreenKeyboardType.Default;

            nameInput.characterLimit =
                Mathf.Max(1, maximumCharacters);

            nameInput.richText = false;
            nameInput.textComponent.richText = false;

            // Evita que el campo cambie de aspecto
            // cuando queda bloqueado al terminar.
            ColorBlock colors = nameInput.colors;
            colors.disabledColor = colors.normalColor;
            nameInput.colors = colors;

            nameInput.readOnly = true;
            nameInput.interactable = false;
            nameInput.gameObject.SetActive(true);

            changeNameButton.onClick.AddListener(
                BeginEditing);

            nameInput.onEndEdit.AddListener(
                EndEditing);

            prepared = true;
        }

        private void Start()
        {
            RefreshName();
        }

        private void OnEnable()
        {
            if (prepared && !editing)
                RefreshName();
        }

        private void RefreshName()
        {
            if (!prepared)
                return;

            ShowName(
                PlayerNamePreferences.Load(
                    playerNameText.text));
        }

        private void ShowName(string value)
        {
            // Mantener actualizado el texto del perfil.
            playerNameText.text = value;

            // Mostrar solamente el texto del Input Field.
            if (playerNameText != nameInput.textComponent)
                playerNameText.enabled = false;

            nameInput.SetTextWithoutNotify(value);

            nameInput.readOnly = true;
            nameInput.interactable = false;

            // El campo permanece visible al terminar.
            nameInput.gameObject.SetActive(true);

            nameInput.textComponent.gameObject.SetActive(true);
            nameInput.textComponent.enabled = true;
            nameInput.textComponent.alpha = 1f;

            nameInput.ForceLabelUpdate();

            changeNameButton.interactable = true;
        }

        public void BeginEditing()
        {
            if (!prepared ||
                editing ||
                !isActiveAndEnabled)
            {
                return;
            }

            originalName =
                PlayerNamePreferences.Load(
                    playerNameText.text);

            editing = true;

            changeNameButton.interactable = false;

            nameInput.SetTextWithoutNotify(originalName);

            nameInput.readOnly = false;
            nameInput.interactable = true;

            focusRoutine = StartCoroutine(FocusInput());
        }

        private IEnumerator FocusInput()
        {
            yield return null;

            focusRoutine = null;

            if (!editing ||
                !nameInput.gameObject.activeInHierarchy)
            {
                yield break;
            }

            nameInput.Select();
            nameInput.ActivateInputField();
        }

        private void EndEditing(string value)
        {
            if (!editing)
                return;

            if (nameInput.wasCanceled)
            {
                FinishEditing(originalName);
                return;
            }

            if (PlayerNamePreferences.TrySave(
                value,
                out string savedName))
            {
                FinishEditing(savedName);
            }
            else
            {
                FinishEditing(originalName);
            }
        }

        private void FinishEditing(string value)
        {
            editing = false;

            if (focusRoutine != null)
            {
                StopCoroutine(focusRoutine);
                focusRoutine = null;
            }

            nameInput.DeactivateInputField();

            ShowName(value);
        }

        private void OnDisable()
        {
            if (prepared && editing)
                FinishEditing(originalName);
        }

        private void OnDestroy()
        {
            if (!prepared)
                return;

            changeNameButton.onClick.RemoveListener(
                BeginEditing);

            nameInput.onEndEdit.RemoveListener(
                EndEditing);
        }
    }
}