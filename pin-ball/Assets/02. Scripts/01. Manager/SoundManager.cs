using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public static class SoundName
{
    public const string MainBgm = "main2";
    public const string Arrow = "arrow_sound";
    public const string BallHit = "ball_hit_sound_lastpart_use";
    public const string BossWind = "boss_wind";
    public const string BumperHit = "bumper_hit_sound";
    public const string ButtonClick = "button_click_sound";
    public const string BuyItem = "Buy_item";
    public const string ClassicPunch = "classic-punch";
    public const string Evolution = "evolution_sound";
    public const string EnemyHealing = "healing2_sound";
    public const string AllyHealing = "healing_sound";
    public const string Hit = "hit_sound";
    public const string MagicSpell = "magicspell_sound";
    public const string SmallPinHit = "pinball_pin_small_hit_sound";
    public const string Spring = "spring_sound";
    public const string SpringPull = "spring_sound2";
    public const string UnitSpawn = "unit_spawn";
    public const string WaveFailed = "wave_faild";
    public const string WaveStart = "wave_start";
    public const string WaveWin = "Wave_Win";

    public static string GetAttack(string unitId)
    {
        return unitId switch
        {
            "archer" or "ranger" => Arrow,
            "mage" or "frost" or "pyromancer" => MagicSpell,
            _ => ClassicPunch
        };
    }
}

public class SoundManager : AppService
{
    [Serializable]
    public struct Sound
    {
        public string Name;
        public AudioClip Clip;
    }
    
    [Header("Audio Clips")]
    [FormerlySerializedAs("_bgmClips")]
    [SerializeField] private Sound[] bgmClips;
    [FormerlySerializedAs("_sfxClips")]
    [SerializeField] private Sound[] sfxClips;
    
    [Header("Audio Sources")]
    [FormerlySerializedAs("_bgmPlayer")]
    [SerializeField] private AudioSource bgmPlayer;
    [FormerlySerializedAs("_sfxPlayer")]
    [SerializeField] private GameObject sfxPlayer;

    [Header("Audio Mixer")]
    [FormerlySerializedAs("_mixer")]
    [SerializeField] private AudioMixer mixer;
    [FormerlySerializedAs("_sfxMixerGroup")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Settings")]
    [SerializeField] private string startupBgmName = SoundName.MainBgm;
    [FormerlySerializedAs("_bgmVolume")]
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.2f;
    [FormerlySerializedAs("_sfxVolume")]
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.2f;
    [FormerlySerializedAs("_initialPoolSize")]
    [SerializeField, Min(1)] private int initialPoolSize = 5;

    private SoundBgmController _bgmController;
    private SoundSfxPoolController _sfxController;
    private SoundMixerController _mixerController;
    private SoundButtonClickController _buttonClickController;

    protected override void Awake()
    {
        base.Awake();
        
        _bgmController = new SoundBgmController(
            bgmClips,
            bgmPlayer,
            bgmVolume);
        _sfxController = new SoundSfxPoolController(
            sfxClips,
            sfxPlayer,
            sfxMixerGroup,
            sfxVolume,
            initialPoolSize);
        _mixerController = new SoundMixerController(mixer);
        _buttonClickController = new SoundButtonClickController(
            () => PlaySFX(SoundName.ButtonClick));
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        _buttonClickController?.Clear();
    }

    private void Start()
    {
        _buttonClickController.RefreshSceneButtons();

        if (!string.IsNullOrWhiteSpace(startupBgmName))
        {
            PlayBGM(startupBgmName);
        }
    }

    private void OnSceneLoaded(Scene _, LoadSceneMode __)
    {
        _buttonClickController.RefreshSceneButtons();
    }

    private void Update()
    {
        _sfxController?.UpdateActiveSources();
    }

    #region BGM
    public void PlayBGM(string name)
    {
        _bgmController.Play(name);
    }

    public void StopBGM() => FadeOutBGM(1);
    
    public void FadeBGM(float targetVolume, float duration)
    {
        _bgmController.Fade(targetVolume, duration);
    }

    public void FadeInBGM(float duration) => FadeBGM(bgmVolume, duration);
    public void FadeOutBGM(float duration) => FadeBGM(0f, duration);
    #endregion

    #region SFX
    public static void PlaySFXIfAvailable(string name)
    {
        if (App.TryGet<SoundManager>(out var soundManager))
        {
            soundManager.PlaySFX(name);
        }
    }

    public AudioSource PlaySFX(string name)
    {
        return _sfxController.Play(name);
    }
    
    public void StopSFX(string name)
    {
        _sfxController.Stop(name);
    }
    
    public void StopAllSFX()
    {
        _sfxController.StopAll();
    }
    #endregion

    #region Mute Toggle

    public void ToggleMute(bool mute)
    {
        _bgmController.ToggleMute(mute);
    }
    public void ToggleMute(EVolumeType type)
    {
        _mixerController.ToggleMute(type);
    }

    public bool IsMuted(EVolumeType type) =>
        _mixerController.IsMuted(type);
    #endregion
}
