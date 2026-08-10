using System.Collections;

using UnityEngine;

[DisallowMultipleComponent]
public sealed class BattleCameraController : MonoBehaviour
{
    [SerializeField] private Vector3 battlePosition = new(0f, 0f, -10f);
    [SerializeField] private Vector3 pinballPosition = new(11.66f, -0.03f, -10f);
    [SerializeField, Min(0f)] private float slideDuration = 0.5f;
    [SerializeField, Min(0f)] private float launchShakeStrength = 0.08f;
    [SerializeField, Min(0f)] private float bumperShakeStrength = 0.12f;
    [SerializeField, Min(0f)] private float goalShakeStrength = 0.16f;

    private BattleManager _battleManager;
    private Coroutine _slideCoroutine;
    private Vector3 _basePosition;
    private float _shakeStrength;
    private float _shakeDuration;
    private float _shakeStartedAt = float.NegativeInfinity;

    private void Start()
    {
        _basePosition = transform.position;
        if (!App.TryGet(out _battleManager))
        {
            Debug.LogError(
                "[BattleCameraController] Missing service: BattleManager");
            enabled = false;
            return;
        }

        _battleManager.OnStateChanged += OnBattleStateChanged;
        ApplyPosition(_battleManager.State, true);
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

    private void OnBattleStateChanged(EWaveState state)
    {
        ApplyPosition(state, false);
    }

    private void ApplyPosition(EWaveState state, bool immediate)
    {
        Vector3 targetPosition = ResolveTargetPosition(
            state,
            battlePosition,
            pinballPosition);

        if (_slideCoroutine != null)
        {
            StopCoroutine(_slideCoroutine);
            _slideCoroutine = null;
        }

        if (immediate || slideDuration <= 0f)
        {
            _basePosition = targetPosition;
            transform.position = _basePosition;
            return;
        }

        _slideCoroutine = StartCoroutine(SlideTo(targetPosition));
    }

    private IEnumerator SlideTo(Vector3 targetPosition)
    {
        Vector3 startPosition = _basePosition;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / slideDuration);
            _basePosition = Vector3.Lerp(
                startPosition,
                targetPosition,
                CalculateEasedProgress(progress));
            yield return null;
        }

        _basePosition = targetPosition;
        transform.position = _basePosition;
        _slideCoroutine = null;
    }

    private void PlayShake(float strength, float duration)
    {
        if (strength <= 0f || duration <= 0f) return;

        _shakeStrength = Mathf.Max(_shakeStrength, strength);
        _shakeDuration = duration;
        _shakeStartedAt = Time.unscaledTime;
    }

    private static Vector3 ResolveTargetPosition(
        EWaveState state,
        Vector3 battlePosition,
        Vector3 pinballPosition)
    {
        return state == EWaveState.Pending
            ? pinballPosition
            : battlePosition;
    }

    private static float CalculateEasedProgress(float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);
        return 1f - Mathf.Pow(1f - clampedProgress, 3f);
    }

    private void OnDestroy()
    {
        if (_battleManager != null)
        {
            _battleManager.OnStateChanged -= OnBattleStateChanged;
        }
    }
}
