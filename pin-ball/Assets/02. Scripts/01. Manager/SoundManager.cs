using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using DG.Tweening;

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
    [SerializeField] private Sound[] _bgmClips;
    [SerializeField] private Sound[] _sfxClips;
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource _bgmPlayer;
    [SerializeField] private GameObject _sfxPlayer;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer _mixer;
    [SerializeField] private AudioMixerGroup _sfxMixerGroup;

    [Header("Settings")]
    [SerializeField] private string startupBgmName = SoundName.MainBgm;
    [SerializeField, Range(0f, 1f)] private float _bgmVolume = 0.2f;
    [SerializeField, Range(0f, 1f)] private float _sfxVolume = 0.2f;
    [SerializeField, Min(1)] private int _initialPoolSize = 5;
    
    private Dictionary<string, AudioClip> _bgmDict = new();
    private Dictionary<string, AudioClip> _sfxDict = new();
    
    private Queue<AudioSource> _sfxPool = new();
    private List<AudioSource> _activeSfx = new();
    private readonly List<Button> _clickSoundButtons = new();
    
    private readonly Dictionary<EVolumeType, bool> _muted = new()
    {
        { EVolumeType.Master, false },
        { EVolumeType.BGM, false },
        { EVolumeType.SFX, false }
    };

    protected override void Awake()
    {
        base.Awake();
        
        foreach (var clip in _bgmClips) _bgmDict[clip.Name] = clip.Clip;
        foreach (var clip in _sfxClips) _sfxDict[clip.Name] = clip.Clip;

        for (var i = 0; i < _initialPoolSize; i++)
        {
            _sfxPool.Enqueue(CreateSfxSource());
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        ClearButtonClickListeners();
    }

    private void Start()
    {
        RegisterButtonClickListeners();

        if (!string.IsNullOrWhiteSpace(startupBgmName))
        {
            PlayBGM(startupBgmName);
        }
    }

    private void OnSceneLoaded(Scene _, LoadSceneMode __)
    {
        RegisterButtonClickListeners();
    }

    private void RegisterButtonClickListeners()
    {
        ClearButtonClickListeners();

        var buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (var button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            button.onClick.AddListener(PlayButtonClickSound);
            _clickSoundButtons.Add(button);
        }
    }

    private void ClearButtonClickListeners()
    {
        foreach (var button in _clickSoundButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayButtonClickSound);
            }
        }

        _clickSoundButtons.Clear();
    }

    private void PlayButtonClickSound()
    {
        PlaySFX(SoundName.ButtonClick);
    }
    
    private void Update()
    {
        for (var i = _activeSfx.Count - 1; i >= 0; i--)
        {
            var source = _activeSfx[i];

            if (source == null)
            {
                _activeSfx.RemoveAt(i);
                continue;
            }

            if (!source.isPlaying && !source.loop)
            {
                ReturnToPool(source);
            }
        }
    }

    #region BGM
    public void PlayBGM(string name)
    {
        if (!_bgmDict.TryGetValue(name, out var clip))
        {
            Debug.LogError($"[SoundManager] BGM '{name}' not found.");
            return;
        }

        if (_bgmPlayer.clip != clip)
        {
            _bgmPlayer.clip = clip;
            _bgmPlayer.volume = 0f;
        }

        _bgmPlayer.loop = true;
        
        FadeInBGM(1f);
    }

    public void StopBGM() => FadeOutBGM(1);
    
    public void FadeBGM(float targetVolume, float duration)
    {
        _bgmPlayer.DOKill();
        
        _bgmPlayer.DOFade(targetVolume, duration)
            .SetEase(Ease.Linear)
            .OnStart(() =>
            {
                if (!_bgmPlayer.isPlaying && targetVolume > 0f)
                    _bgmPlayer.Play();
            })
            .OnComplete(() =>
            {
                if (Mathf.Approximately(targetVolume, 0f))
                    _bgmPlayer.Stop();
            });
    }

    public void FadeInBGM(float duration) => FadeBGM(_bgmVolume, duration);
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
        if (!_sfxDict.TryGetValue(name, out var clip))
        {
            Debug.LogError($"[SoundManager] SFX '{name}' not found.");
            return null;
        }
        
        var src = _sfxPool.Count > 0
            ? _sfxPool.Dequeue()
            : CreateSfxSource();

        src.playOnAwake = false;
        src.clip = clip;
        src.loop = false;
        src.outputAudioMixerGroup = _sfxMixerGroup;
        src.volume = _sfxVolume;
        src.Play();

        _activeSfx.Add(src);
        
        return src;
    }
    
    public void StopSFX(string name)
    {
        if (!_sfxDict.TryGetValue(name, out var clip))
        {
            Debug.LogError($"[SoundManager] SFX '{name}' not found.");
            return;
        }

        foreach (var source in _activeSfx)
        {
            if (source.clip != clip)
            {
                continue;
            }

            source.DOKill();

            source
                .DOFade(0f, 1f)
                .SetEase(Ease.Linear)
                .OnComplete(() => ReturnToPool(source));
        }
    }
    
    public void StopAllSFX()
    {
        for (var i = _activeSfx.Count - 1; i >= 0; i--)
        {
            ReturnToPool(_activeSfx[i]);
        }
    }
    
    private AudioSource CreateSfxSource()
    {
        var src = _sfxPlayer.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.outputAudioMixerGroup = _sfxMixerGroup;
        return src;
    }
    
    private void ReturnToPool(AudioSource source)
    {
        if (!_activeSfx.Remove(source)) return;

        source.DOKill();
        source.Stop();
        source.clip = null;
        source.loop = false;
        source.volume = _sfxVolume;

        _sfxPool.Enqueue(source);
    }
    #endregion

    #region Mute Toggle

    public void ToggleMute(bool mute)
    {
        _bgmPlayer.volume = mute ? 0f : _bgmVolume;
    }
    public void ToggleMute(EVolumeType type)
    {
        _muted[type] = !_muted[type];
        var db = _muted[type] ? -80f : 0f;
        _mixer.SetFloat(ParamName(type), db);
    }
    
    private string ParamName(EVolumeType t) => t switch
    {
        EVolumeType.Master => "Master_Vol",
        EVolumeType.BGM    => "BGM_Vol",
        EVolumeType.SFX    => "SFX_Vol",
        _ => "Master_Vol",
    };

    public bool IsMuted(EVolumeType type) => _muted[type];
    #endregion
}
