using UnityEngine;

[DisallowMultipleComponent]
public sealed class BattleCameraController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float launchShakeStrength = 0.08f;
    [SerializeField, Min(0f)] private float bumperShakeStrength = 0.12f;
    [SerializeField, Min(0f)] private float goalShakeStrength = 0.16f;

    private Vector3 _basePosition;
    private float _shakeStrength;
    private float _shakeDuration;
    private float _shakeStartedAt = float.NegativeInfinity;

    private void Start()
    {
        _basePosition = transform.position;
    }

    private void LateUpdate()
    {
        float elapsed = Time.unscaledTime - _shakeStartedAt;
        if (_shakeDuration <= 0f || elapsed >= _shakeDuration)
        {
            _shakeStrength = 0f;
            _shakeDuration = 0f;
            transform.position = _basePosition;
            return;
        }

        float fade = 1f - elapsed / _shakeDuration;
        float phase = elapsed * 95f;
        var offset = new Vector3(
            Mathf.Sin(phase * 1.13f),
            Mathf.Cos(phase * 0.91f),
            0f) * (_shakeStrength * fade);
        transform.position = _basePosition + offset;
    }

    public void PlayPinballLaunchShake(float normalizedPull)
    {
        PlayShake(launchShakeStrength * Mathf.Lerp(0.65f, 1f, Mathf.Clamp01(normalizedPull)), 0.12f);
    }

    public void PlayPinballBumperShake()
    {
        PlayShake(bumperShakeStrength, 0.1f);
    }

    public void PlayPinballGoalShake()
    {
        PlayShake(goalShakeStrength, 0.16f);
    }

    public void PlayBattleImpactShake(bool strong)
    {
        PlayShake(strong ? 0.1f : 0.055f, strong ? 0.13f : 0.08f);
    }

    private void PlayShake(float strength, float duration)
    {
        if (strength <= 0f || duration <= 0f) return;

        _shakeStrength = Mathf.Max(_shakeStrength, strength);
        _shakeDuration = duration;
        _shakeStartedAt = Time.unscaledTime;
    }

}
