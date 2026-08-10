using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldHealthBarController : MonoBehaviour
{
    [SerializeField] private UnitBase ownerUnit;
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private SpriteRenderer delayedFillRenderer;
    [SerializeField] private SpriteRenderer currentFillRenderer;
    [SerializeField] private SpriteRenderer frameRenderer;
    [SerializeField, Min(0f)] private float damageDelay = 0.25f;
    [SerializeField, Min(0f)] private float delayedFollowSpeed = 1.5f;

    private Vector3 _currentFullScale;
    private Vector3 _delayedFullScale;
    private float _currentRatio;
    private float _delayedRatio;
    private float _remainingDamageDelay;
    private bool _needsSynchronization;

    private void Awake()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError(
                "[WorldHealthBarController] Missing Inspector reference.",
                this);
            enabled = false;
            return;
        }

        _currentFullScale = currentFillRenderer.transform.localScale;
        _delayedFullScale = delayedFillRenderer.transform.localScale;
        _needsSynchronization = true;
    }

    private void OnEnable()
    {
        _needsSynchronization = true;
    }

    private void LateUpdate()
    {
        float nextRatio = ClampRatio(ownerUnit.HpRatio);
        if (_needsSynchronization)
        {
            Synchronize(nextRatio);
            return;
        }

        if (nextRatio < _currentRatio)
        {
            _remainingDamageDelay = damageDelay;
        }

        _remainingDamageDelay = Mathf.Max(
            0f,
            _remainingDamageDelay - Time.deltaTime);
        _delayedRatio = CalculateDelayedRatio(
            nextRatio,
            _delayedRatio,
            _remainingDamageDelay,
            delayedFollowSpeed,
            Time.deltaTime);
        _currentRatio = nextRatio;
        ApplyScales();
    }

    private void Synchronize(float ratio)
    {
        _currentRatio = ratio;
        _delayedRatio = ratio;
        _remainingDamageDelay = 0f;
        _needsSynchronization = false;
        ApplyScales();
    }

    private void ApplyScales()
    {
        currentFillRenderer.transform.localScale = new Vector3(
            _currentFullScale.x * _currentRatio,
            _currentFullScale.y,
            _currentFullScale.z);
        delayedFillRenderer.transform.localScale = new Vector3(
            _delayedFullScale.x * _delayedRatio,
            _delayedFullScale.y,
            _delayedFullScale.z);
    }

    private bool HasRequiredReferences()
    {
        return ownerUnit != null &&
               backgroundRenderer != null &&
               delayedFillRenderer != null &&
               currentFillRenderer != null &&
               frameRenderer != null;
    }

    private static float ClampRatio(float ratio)
    {
        return Mathf.Clamp01(ratio);
    }

    private static float CalculateDelayedRatio(
        float currentRatio,
        float delayedRatio,
        float remainingDelay,
        float followSpeed,
        float deltaTime)
    {
        currentRatio = ClampRatio(currentRatio);
        delayedRatio = ClampRatio(delayedRatio);

        if (currentRatio >= delayedRatio)
        {
            return currentRatio;
        }

        if (remainingDelay > 0f)
        {
            return delayedRatio;
        }

        return Mathf.MoveTowards(
            delayedRatio,
            currentRatio,
            Mathf.Max(0f, followSpeed) * Mathf.Max(0f, deltaTime));
    }
}
