using System.Collections.Generic;

using UnityEngine.Audio;

internal sealed class SoundMixerController
{
    private readonly AudioMixer _mixer;
    private readonly Dictionary<EVolumeType, bool> _muted = new()
    {
        { EVolumeType.Master, false },
        { EVolumeType.BGM, false },
        { EVolumeType.SFX, false }
    };

    public SoundMixerController(AudioMixer mixer)
    {
        _mixer = mixer;
    }

    public void ToggleMute(EVolumeType type)
    {
        _muted[type] = !_muted[type];
        _mixer.SetFloat(GetParameterName(type), _muted[type] ? -80f : 0f);
    }

    public bool IsMuted(EVolumeType type) => _muted[type];

    private static string GetParameterName(EVolumeType type) => type switch
    {
        EVolumeType.Master => "Master_Vol",
        EVolumeType.BGM => "BGM_Vol",
        EVolumeType.SFX => "SFX_Vol",
        _ => "Master_Vol"
    };
}
