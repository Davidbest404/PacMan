using System;
using UnityEngine;

/// <summary>Хранит громкость музыки и SFX между запусками (PlayerPrefs).</summary>
public static class AudioSettings
{
    private const string MusicKey = "audio_music_volume";
    private const string SfxKey = "audio_sfx_volume";

    private static float musicVolume = 0.7f;
    private static float sfxVolume = 1.0f;

    public static event Action OnChanged;

    public static float MusicVolume
    {
        get => musicVolume;
        set
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(musicVolume, value)) return;
            musicVolume = value;
            PlayerPrefs.SetFloat(MusicKey, musicVolume);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static float SfxVolume
    {
        get => sfxVolume;
        set
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(sfxVolume, value)) return;
            sfxVolume = value;
            PlayerPrefs.SetFloat(SfxKey, sfxVolume);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static void Load()
    {
        musicVolume = PlayerPrefs.GetFloat(MusicKey, 0.7f);
        sfxVolume = PlayerPrefs.GetFloat(SfxKey, 1.0f);
    }
}