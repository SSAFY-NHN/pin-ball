using UnityEngine;

[DisallowMultipleComponent]
public sealed class BattleMoonlightController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer moonlightRenderer;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.8f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.4f;

    private BattleManager _battleManager;
    private Color _baseColor;
    private float _targetAlpha;

    private void Start()
    {
        if (moonlightRenderer == null)
        {
            Debug.LogError(
                "[BattleMoonlightController] Missing moonlight renderer.");
            enabled = false;
            return;
        }

        if (!App.TryGet(out _battleManager))
        {
            Debug.LogError(
                "[BattleMoonlightController] Missing service: BattleManager");
            enabled = false;
            return;
        }

        _baseColor = moonlightRenderer.color;
        _battleManager.OnStateChanged += OnBattleStateChanged;
        ApplyState(_battleManager.State, true);
    }

    private void Update()
    {
        Color color = moonlightRenderer.color;
        if (Mathf.Approximately(color.a, _targetAlpha)) return;

        float duration = _targetAlpha > color.a
            ? fadeInDuration
            : fadeOutDuration;
        float nextAlpha = duration <= 0f
            ? _targetAlpha
            : Mathf.MoveTowards(
                color.a,
                _targetAlpha,
                _baseColor.a * Time.unscaledDeltaTime / duration);

        color.a = nextAlpha;
        moonlightRenderer.color = color;
        moonlightRenderer.enabled = nextAlpha > 0f;
    }

    private void OnBattleStateChanged(EWaveState state)
    {
        ApplyState(state, false);
    }

    private void ApplyState(EWaveState state, bool immediate)
    {
        _targetAlpha = state == EWaveState.Active ? _baseColor.a : 0f;
        if (!immediate) return;

        Color color = _baseColor;
        color.a = _targetAlpha;
        moonlightRenderer.color = color;
        moonlightRenderer.enabled = _targetAlpha > 0f;
    }

    private void OnDestroy()
    {
        if (_battleManager != null)
        {
            _battleManager.OnStateChanged -= OnBattleStateChanged;
        }
    }
}
