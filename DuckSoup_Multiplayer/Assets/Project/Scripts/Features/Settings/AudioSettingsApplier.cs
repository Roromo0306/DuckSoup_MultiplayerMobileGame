using UnityEngine;
using UnityEngine.Audio;

namespace DuckSoup.Features.Settings
{
    // Agregar en las escenas que tengan musica o SFX para restaurar PlayerPrefs.
    public sealed class AudioSettingsApplier : MonoBehaviour
    {
        [SerializeField] private AudioMixer audioMixer;
        private void Awake()
        {
            if (audioMixer != null) new AudioSettingsService(audioMixer);
            else Debug.LogWarning("[AudioSettings] Asigna DuckSoupMixer.", this);
        }
    }
}
