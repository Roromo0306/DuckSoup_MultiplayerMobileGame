using DuckSoup.Features.PowerUps;
using UnityEngine;

namespace DuckSoup.Features.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class DuckSoupSoundPlayer : MonoBehaviour
    {
        [Header("Audio Source - DuckSoupMixer / SFX")]
        [SerializeField] private AudioSource sfxSource;

        [Header("Main Menu")]
        [SerializeField] private AudioClip buttonClick;
        [SerializeField] private AudioClip closeWindow;

        [Header("Countdown")]
        [SerializeField] private AudioClip countdownSequence;
        [SerializeField] private AudioClip countdownTick;
        [SerializeField] private AudioClip countdownGo;

        [Header("Gameplay")]
        [SerializeField] private AudioClip ingredientTap;
        [SerializeField] private AudioClip correctIngredient;
        [SerializeField] private AudioClip incorrectIngredient;
        [SerializeField] private AudioClip recipeCompleted;
        [SerializeField] private AudioClip timeUp;

        [Header("Card lock - Multiplayer")]
        [SerializeField] private AudioClip cardLocked;

        [Header("Combos - Both Modes")]
        [SerializeField] private AudioClip comboX2;
        [SerializeField] private AudioClip comboX3;

        [Header("Special Tokens - Both Modes")]
        [SerializeField] private AudioClip wildcardDuckUsed;
        [SerializeField] private AudioClip poopUsed;
        [SerializeField] private AudioClip completeRecipeUsed;
        [SerializeField] private AudioClip extraComboTimeUsed;

        [Header("Two Player Results")]
        [SerializeField] private AudioClip winner;
        [SerializeField] private AudioClip confetti;

        [Header("Duck Voices")]
        [SerializeField]
        private AudioClip[] duckVoices = new AudioClip[4];

        [Header("Debug")]
        [SerializeField] private bool warnAboutMissingClips;

        private static DuckSoupSoundPlayer instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            if (sfxSource == null)
                sfxSource = GetComponent<AudioSource>();

            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.pitch = 1f;

            DontDestroyOnLoad(gameObject);
        }

        public static void Play(DuckSoupSoundId soundId)
        {
            if (instance == null)
                return;

            AudioClip clip = instance.GetClip(soundId);

            instance.PlayClip(
                clip,
                soundId.ToString());
        }

        public static void PlayCombo(int multiplier)
        {
            if (multiplier == 2)
                Play(DuckSoupSoundId.ComboX2);
            else if (multiplier == 3)
                Play(DuckSoupSoundId.ComboX3);
        }

        public static void PlayPowerUp(PowerTokenKind kind)
        {
            switch (kind)
            {
                case PowerTokenKind.WildcardDuck:
                    Play(DuckSoupSoundId.WildcardDuckUsed);
                    break;

                case PowerTokenKind.Poison:
                    // Poison es el nombre interno que ya usa la caca.
                    Play(DuckSoupSoundId.PoopUsed);
                    break;

                case PowerTokenKind.CompleteRecipe:
                    Play(DuckSoupSoundId.CompleteRecipeUsed);
                    break;

                case PowerTokenKind.ExtraComboTime:
                    Play(DuckSoupSoundId.ExtraComboTimeUsed);
                    break;
            }
        }

        public static void PlayDuck(int duckIndex)
        {
            if (instance == null)
                return;

            if (instance.duckVoices == null ||
                duckIndex < 0 ||
                duckIndex >= instance.duckVoices.Length)
            {
                Debug.LogWarning(
                    "[DuckSoupAudio] Invalid duck index: " +
                    duckIndex);

                return;
            }

            AudioClip clip = instance.duckVoices[duckIndex];

            instance.PlayClip(
                clip,
                "Duck " + (duckIndex + 1));
        }

        private void PlayClip(AudioClip clip, string label)
        {
            if (sfxSource == null)
                return;

            if (clip == null)
            {
                if (warnAboutMissingClips)
                {
                    Debug.LogWarning(
                        "[DuckSoupAudio] Missing clip: " + label,
                        this);
                }

                return;
            }

            // Permite varios efectos simultaneos.
            sfxSource.PlayOneShot(clip);
        }

        private AudioClip GetClip(DuckSoupSoundId id)
        {
            switch (id)
            {
                case DuckSoupSoundId.ButtonClick:
                    return buttonClick;

                case DuckSoupSoundId.CloseWindow:
                    return closeWindow;

                case DuckSoupSoundId.CountdownSequence:
                    return countdownSequence;

                case DuckSoupSoundId.CountdownTick:
                    return countdownTick;

                case DuckSoupSoundId.CountdownGo:
                    return countdownGo;

                case DuckSoupSoundId.IngredientTap:
                    return ingredientTap;

                case DuckSoupSoundId.CorrectIngredient:
                    return correctIngredient;

                case DuckSoupSoundId.IncorrectIngredient:
                    return incorrectIngredient;

                case DuckSoupSoundId.RecipeCompleted:
                    return recipeCompleted;

                case DuckSoupSoundId.TimeUp:
                    return timeUp;

                case DuckSoupSoundId.Winner:
                    return winner;

                case DuckSoupSoundId.Confetti:
                    return confetti;

                case DuckSoupSoundId.CardLocked:
                    return cardLocked;

                case DuckSoupSoundId.ComboX2:
                    return comboX2;

                case DuckSoupSoundId.ComboX3:
                    return comboX3;

                case DuckSoupSoundId.WildcardDuckUsed:
                    return wildcardDuckUsed;

                case DuckSoupSoundId.PoopUsed:
                    return poopUsed;

                case DuckSoupSoundId.CompleteRecipeUsed:
                    return completeRecipeUsed;

                case DuckSoupSoundId.ExtraComboTimeUsed:
                    return extraComboTimeUsed;

                default:
                    return null;
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}