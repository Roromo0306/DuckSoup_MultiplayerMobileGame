using System;
using UnityEngine;
using UnityEngine.Audio;

namespace DuckSoup.Features.Settings
{
    // Volumenes persistentes 0..1, enviados a grupos de un AudioMixer.
    public sealed class AudioSettingsService
    {
        private const string KeyMaster = "DuckSoup_Audio_Master";
        private const string KeyMusic = "DuckSoup_Audio_Music";
        private const string KeySfx = "DuckSoup_Audio_SFX";
        private readonly AudioMixer mixer;

        public float Master => PlayerPrefs.GetFloat(KeyMaster, 1f);
        public float Music => PlayerPrefs.GetFloat(KeyMusic, 1f);
        public float Sfx => PlayerPrefs.GetFloat(KeySfx, 1f);

        public AudioSettingsService(AudioMixer audioMixer)
        {
            mixer = audioMixer ?? throw new ArgumentNullException(nameof(audioMixer));
            Apply();
        }

        public void SetMaster(float value) => Store(KeyMaster, "MasterVolume", value);
        public void SetMusic(float value) => Store(KeyMusic, "MusicVolume", value);
        public void SetSfx(float value) => Store(KeySfx, "SFXVolume", value);

        private void Store(string key, string mixerParameter, float value)
        {
            float clamped = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(key, clamped);
            PlayerPrefs.Save();
            SetMixerParameter(mixerParameter, clamped);
        }

        public void Apply()
        {
            SetMixerParameter("MasterVolume", Master);
            SetMixerParameter("MusicVolume", Music);
            SetMixerParameter("SFXVolume", Sfx);
        }

        private void SetMixerParameter(string name, float value)
        {
            if (!mixer.SetFloat(name, ToDb(value)))
                Debug.LogWarning("[AudioSettings] Expone en AudioMixer el parametro: " + name);
        }

        // Para que 0 sea silencio real en un Mixer logaritmico.
        private static float ToDb(float value)
        {
            return value <= 0.0001f ? -80f : 20f * Mathf.Log10(value);
        }
    }
}
