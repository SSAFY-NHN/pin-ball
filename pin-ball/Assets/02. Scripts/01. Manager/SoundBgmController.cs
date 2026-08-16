using System.Collections.Generic;

using DG.Tweening;
using UnityEngine;

internal sealed class SoundBgmController
{
    private readonly Dictionary<string, AudioClip> _clips = new();
    private readonly AudioSource _player;
    private readonly float _volume;

    public SoundBgmController(
        SoundManager.Sound[] sounds,
        AudioSource player,
        float volume)
    {
        _player = player;
        _volume = volume;

        foreach (var sound in sounds)
        {
            _clips[sound.Name] = sound.Clip;
        }
    }

    public void Play(string name)
    {
        if (!_clips.TryGetValue(name, out var clip))
        {
            Debug.LogError($"[SoundManager] BGM '{name}' not found.");
            return;
        }

        if (_player.clip != clip)
        {
            _player.clip = clip;
            _player.volume = 0f;
        }

        _player.loop = true;
        Fade(_volume, 1f);
    }

    public void Fade(float targetVolume, float duration)
    {
        _player.DOKill();
        _player.DOFade(targetVolume, duration)
            .SetEase(Ease.Linear)
            .OnStart(() =>
            {
                if (!_player.isPlaying && targetVolume > 0f)
                {
                    _player.Play();
                }
            })
            .OnComplete(() =>
            {
                if (Mathf.Approximately(targetVolume, 0f))
                {
                    _player.Stop();
                }
            });
    }

    public void ToggleMute(bool mute)
    {
        _player.volume = mute ? 0f : _volume;
    }
}
