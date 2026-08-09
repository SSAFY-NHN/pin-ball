using System.Collections;

using UnityEngine;

[DisallowMultipleComponent]
public sealed class BattleCameraController : MonoBehaviour
{
    [SerializeField] private Vector3 battlePosition = new(0f, 0f, -10f);
    [SerializeField] private Vector3 pinballPosition = new(11.66f, -0.03f, -10f);
    [SerializeField, Min(0f)] private float slideDuration = 0.5f;

    private BattleManager _battleManager;
    private Coroutine _slideCoroutine;

    private void Start()
    {
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
            transform.position = targetPosition;
            return;
        }

        _slideCoroutine = StartCoroutine(SlideTo(targetPosition));
    }

    private IEnumerator SlideTo(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / slideDuration);
            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                CalculateEasedProgress(progress));
            yield return null;
        }

        transform.position = targetPosition;
        _slideCoroutine = null;
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
