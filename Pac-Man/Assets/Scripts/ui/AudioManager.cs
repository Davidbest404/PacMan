using UnityEngine;

/// <summary>
/// Центральный менеджер звука. Автоматически создаётся GameManager'ом.
/// Разделение: musicSource (loop музыка), sfxSource (короткие PlayOneShot),
/// chompSource (loop-звук жевания Пакмана).
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music (loop)")]
    public AudioClip menuMusic;
    public AudioClip gameMusic;
    public AudioClip frightenedMusic;

    [Header("Jingles (короткие)")]
    public AudioClip gameStartJingle;
    public AudioClip winJingle;
    public AudioClip loseJingle;

    [Header("SFX")]
    public AudioClip pelletSound;
    public AudioClip powerPelletSound;
    public AudioClip eatGhostSound;
    public AudioClip bonusSpawnSound;
    public AudioClip bonusCollectSound;
    public AudioClip deathSound;
    public AudioClip chompSound;
    public AudioClip buttonClickSound;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioSource chompSource;

    private AudioClip currentMusic;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        chompSource = gameObject.AddComponent<AudioSource>();
        chompSource.loop = true;
        chompSource.playOnAwake = false;
    }

    private void OnEnable() { AudioSettings.OnChanged += ApplyVolumes; }
    private void OnDisable() { AudioSettings.OnChanged -= ApplyVolumes; }

    public void Init()
    {
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (musicSource != null) musicSource.volume = AudioSettings.MusicVolume;
        if (sfxSource != null) sfxSource.volume = AudioSettings.SfxVolume;
        if (chompSource != null) chompSource.volume = AudioSettings.SfxVolume * 0.8f;
    }

    // ───────── Музыка ─────────

    public void PlayMenuMusic() => PlayMusic(menuMusic);
    public void PlayGameMusic() => PlayMusic(gameMusic);
    public void PlayFrightenedMusic() => PlayMusic(frightenedMusic);

    private void PlayMusic(AudioClip clip)
    {
        if (musicSource == null) return;
        if (clip == null)
        {
            musicSource.Stop();
            currentMusic = null;
            return;
        }
        if (currentMusic == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.Play();
        currentMusic = clip;
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
        currentMusic = null;
    }

    public void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying) musicSource.Pause();
    }

    public void ResumeMusic()
    {
        if (musicSource != null && musicSource.clip != null && !musicSource.isPlaying)
            musicSource.UnPause();
    }

    // ───────── Джинглы (проигрываются как SFX, музыка не трогается) ─────────

    public void PlayGameStartJingle() => PlayOneShot(gameStartJingle);
    public void PlayWinJingle() => PlayOneShot(winJingle);
    public void PlayLoseJingle() => PlayOneShot(loseJingle);

    // ───────── SFX ─────────

    public void PlayPellet() => PlayOneShot(pelletSound);
    public void PlayPowerPellet() => PlayOneShot(powerPelletSound);
    public void PlayEatGhost() => PlayOneShot(eatGhostSound);
    public void PlayBonusSpawn() => PlayOneShot(bonusSpawnSound);
    public void PlayBonusCollect() => PlayOneShot(bonusCollectSound);
    public void PlayDeath() => PlayOneShot(deathSound);
    public void PlayButtonClick() => PlayOneShot(buttonClickSound);

    public void PlayOneShot(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }

    // ───────── Chomp (звук жевания) ─────────

    public void StartChomp()
    {
        if (chompSound == null || chompSource == null) return;
        if (chompSource.isPlaying) return;
        chompSource.clip = chompSound;
        chompSource.Play();
    }

    public void StopChomp()
    {
        if (chompSource != null && chompSource.isPlaying)
            chompSource.Stop();
    }
    public float GetGameStartJingleDuration()
    {
        return gameStartJingle != null ? gameStartJingle.length : 0f;
    }
}