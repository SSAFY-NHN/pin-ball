using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Audio;

using DG.Tweening;

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
    [SerializeField, Range(0f, 1f)] private float _bgmVolume = 0.2f;
    [SerializeField, Range(0f, 1f)] private float _sfxVolume = 0.2f;
    [SerializeField, Min(1)] private int _initialPoolSize = 5;
    
    private Dictionary<string, AudioClip> _bgmDict = new();
    private Dictionary<string, AudioClip> _sfxDict = new();
    
    private Queue<AudioSource> _sfxPool = new();
    private List<AudioSource> _activeSfx = new();
    
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

        _bgmPlayer.clip = clip;
        _bgmPlayer.loop = true;
        
        FadeInBGM(5);
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

    public void FadeInBGM(float duration) => FadeBGM(0.2f, duration);
    public void FadeOutBGM(float duration) => FadeBGM(0f, duration);
    #endregion

    #region SFX
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
        src.outputAudioMixerGroup =
            _mixer.FindMatchingGroups("SFX")[0];
        src.volume = 0.2f;
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
        _activeSfx.Add(src);
        return src;
    }
    
    private void ReturnToPool(AudioSource source)
    {
        if (!_activeSfx.Remove(source)) return;

        source.DOKill();
        source.Stop();
        source.clip = null;
        source.loop = false;
        source.volume = 0.2f;

        _sfxPool.Enqueue(source);
    }
    #endregion

    #region Mute Toggle

    public void ToggleMute(bool mute)
    {
        _bgmPlayer.volume = mute ? 0f : 0.2f;
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