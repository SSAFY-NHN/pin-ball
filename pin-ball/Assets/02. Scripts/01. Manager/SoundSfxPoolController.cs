using System.Collections.Generic;

using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

internal sealed class SoundSfxPoolController
{
    private readonly Dictionary<string, AudioClip> _clips = new();
    private readonly Queue<AudioSource> _availableSources = new();
    private readonly List<AudioSource> _activeSources = new();
    private readonly GameObject _sourceOwner;
    private readonly AudioMixerGroup _mixerGroup;
    private readonly float _volume;

    public SoundSfxPoolController(
        SoundManager.Sound[] sounds,
        GameObject sourceOwner,
        AudioMixerGroup mixerGroup,
        float volume,
        int initialPoolSize)
    {
        _sourceOwner = sourceOwner;
        _mixerGroup = mixerGroup;
        _volume = volume;

        foreach (var sound in sounds)
        {
            _clips[sound.Name] = sound.Clip;
        }

        for (var i = 0; i < initialPoolSize; i++)
        {
            _availableSources.Enqueue(CreateSource());
        }
    }

    public AudioSource Play(string name)
    {
        if (!_clips.TryGetValue(name, out var clip))
        {
            Debug.LogError($"[SoundManager] SFX '{name}' not found.");
            return null;
        }

        var source = _availableSources.Count > 0
            ? _availableSources.Dequeue()
            : CreateSource();

        source.playOnAwake = false;
        source.clip = clip;
        source.loop = false;
        source.outputAudioMixerGroup = _mixerGroup;
        source.volume = _volume;
        source.Play();
        _activeSources.Add(source);
        return source;
    }

    public void Stop(string name)
    {
        if (!_clips.TryGetValue(name, out var clip))
        {
            Debug.LogError($"[SoundManager] SFX '{name}' not found.");
            return;
        }

        foreach (var source in _activeSources.ToArray())
        {
            if (source.clip != clip) continue;

            source.DOKill();
            source.DOFade(0f, 1f)
                .SetEase(Ease.Linear)
                .OnComplete(() => Return(source));
        }
    }

    public void StopAll()
    {
        for (var i = _activeSources.Count - 1; i >= 0; i--)
        {
            Return(_activeSources[i]);
        }
    }

    public void UpdateActiveSources()
    {
        for (var i = _activeSources.Count - 1; i >= 0; i--)
        {
            var source = _activeSources[i];
            if (source == null)
            {
                _activeSources.RemoveAt(i);
            }
            else if (!source.isPlaying && !source.loop)
            {
                Return(source);
            }
        }
    }

    private AudioSource CreateSource()
    {
        var source = _sourceOwner.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.outputAudioMixerGroup = _mixerGroup;
        return source;
    }

    private void Return(AudioSource source)
    {
        if (!_activeSources.Remove(source)) return;

        source.DOKill();
        source.Stop();
        source.clip = null;
        source.loop = false;
        source.volume = _volume;
        _availableSources.Enqueue(source);
    }
}
