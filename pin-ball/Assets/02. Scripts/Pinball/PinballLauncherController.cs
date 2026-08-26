using System.Collections;

using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PinballLauncherController : MonoBehaviour
{
    public const bool ManualInputEnabled = false;
    [SerializeField] private PinballManager pinballManager;
    [SerializeField] private Transform loadPoint;
    [SerializeField] private Transform piston;
    [SerializeField] private Transform spring;
    [SerializeField] private PinballLauncherGlowController glowController;
    [SerializeField] private Vector2 launchDirection = Vector2.up;
    [SerializeField, Min(0.1f)] private float maximumPullDistance = 1.1f;
    [SerializeField, Range(0f, 1f)] private float minimumPullRatio = 0.1f;
    [SerializeField, Range(0f, 60f)] private float leverMaximumAngle = 28f;
    [SerializeField, Range(0f, 1f)] private float pistonTravelRatio = 0.65f;
    [SerializeField, Min(0.01f)] private float snapDuration = 0.11f;

    public Vector2 LoadPosition => loadPoint != null
        ? loadPoint.position
        : transform.position;
    public Vector2 LaunchDirection => launchDirection.sqrMagnitude > 0.001f
        ? launchDirection.normalized
        : Vector2.up;

    private Camera _camera;
    private Vector3 _leverStartPosition;
    private Vector3 _pistonStartPosition;
    private Vector3 _loadPointStartPosition;
    private Vector3 _springStartPosition;
    private Vector3 _springStartScale;
    private Quaternion _leverStartRotation;
    private Vector3 _leverPivotInParent;
    private float _springOriginalHeight;
    private float _pointerStartY;
    private float _pullDistance;
    private bool _isDragging;
    private bool _hasPlayedPullSound;
    private bool _hasLoadedBall;
    private bool _crossedMinimumPull;
    private float _leverRotationDirection;
    private Coroutine _snapCoroutine;

    private void Awake()
    {
        _camera = Camera.main;
        _leverStartPosition = transform.localPosition;
        _leverStartRotation = transform.localRotation;
        var localForward = _leverStartRotation * Vector3.forward;
        _leverRotationDirection =
            Vector3.Dot(localForward, Vector3.forward) < 0f ? 1f : -1f;
        if (piston != null) _pistonStartPosition = piston.localPosition;
        if (loadPoint != null) _loadPointStartPosition = loadPoint.localPosition;
        if (spring != null)
        {
            _springStartPosition = spring.localPosition;
            _springStartScale = spring.localScale;
            var renderer = spring.GetComponent<SpriteRenderer>();
            _springOriginalHeight = renderer != null && renderer.sprite != null
                ? renderer.sprite.bounds.size.y * Mathf.Abs(_springStartScale.y)
                : 1f;
            glowController?.InitializeSpring(renderer);
        }

        var leverRenderer = GetComponent<SpriteRenderer>();
        var localPivot = leverRenderer != null && leverRenderer.sprite != null
            ? leverRenderer.sprite.bounds.min +
              new Vector3(leverRenderer.sprite.bounds.size.x * 0.18f,
                  leverRenderer.sprite.bounds.size.y * 0.18f)
            : Vector3.zero;
        var pivotWorld = transform.TransformPoint(localPivot);
        _leverPivotInParent = transform.parent != null
            ? transform.parent.InverseTransformPoint(pivotWorld)
            : pivotWorld;
    }

    private void OnMouseDown()
    {
        if (!ManualInputEnabled) return;
        if (!_hasLoadedBall) return;
        StopSnap();
        _isDragging = true;
        _hasPlayedPullSound = false;
        _pointerStartY = GetPointerWorldY();
        _pullDistance = 0f;
        _crossedMinimumPull = false;
    }

    private void OnMouseEnter()
    {
        if (!ManualInputEnabled) return;
        glowController?.SetHovered(_hasLoadedBall);
    }

    private void OnMouseExit()
    {
        if (!ManualInputEnabled) return;
        if (!_isDragging) glowController?.SetHovered(false);
    }

    private void OnMouseDrag()
    {
        if (!ManualInputEnabled) return;
        if (!_isDragging) return;

        _pullDistance = Mathf.Clamp(
            _pointerStartY - GetPointerWorldY(),
            0f,
            maximumPullDistance);
        if (ShouldPlayPullSound(_hasPlayedPullSound, _pullDistance))
        {
            _hasPlayedPullSound = true;
            SoundManager.PlaySFXIfAvailable(SoundName.SpringPull);
        }

        var pullRatio = maximumPullDistance > 0f
            ? _pullDistance / maximumPullDistance
            : 0f;
        if (!_crossedMinimumPull && pullRatio >= minimumPullRatio)
        {
            _crossedMinimumPull = true;
            glowController?.PlayThresholdReached();
        }
        else if (_crossedMinimumPull && pullRatio < minimumPullRatio)
        {
            _crossedMinimumPull = false;
        }
        ApplyVisualPull(_pullDistance);
    }

    private void OnMouseUp()
    {
        if (!ManualInputEnabled) return;
        if (!_isDragging) return;
        _isDragging = false;

        var pullRatio = _pullDistance / maximumPullDistance;
        var launched = pullRatio >= minimumPullRatio &&
                       pinballManager != null &&
                       pinballManager.TryLaunchLoadedBall(pullRatio);
        if (launched)
        {
            glowController?.PlayLaunch();
            SetLoaded(false);
        }

        StartSnap();
    }

    private void OnDisable()
    {
        _isDragging = false;
        _hasPlayedPullSound = false;
        StopSnap();
        ResetVisualsImmediate();
        glowController?.ResetInteraction();
    }

    public void SetLoaded(bool isLoaded)
    {
        _hasLoadedBall = isLoaded;
        glowController?.SetLoaded(isLoaded);
    }

    private static bool ShouldPlayPullSound(
        bool hasPlayedPullSound,
        float pullDistance)
    {
        return !hasPlayedPullSound && pullDistance > 0f;
    }

    private float GetPointerWorldY()
    {
        if (_camera == null) _camera = Camera.main;
        if (_camera == null) return transform.position.y;

        var pointer = Input.mousePosition;
        pointer.z = Mathf.Abs(_camera.transform.position.z - transform.position.z);
        return _camera.ScreenToWorldPoint(pointer).y;
    }

    private void ApplyVisualPull(float distance)
    {
        var pullRatio = maximumPullDistance > 0f
            ? distance / maximumPullDistance
            : 0f;
        glowController?.SetPullRatio(pullRatio);
        var leverAngle = leverMaximumAngle *
                         pullRatio *
                         _leverRotationDirection;
        var rotatedOffset = Quaternion.Euler(0f, 0f, leverAngle) *
                            (_leverStartPosition - _leverPivotInParent);
        transform.localPosition = _leverPivotInParent + rotatedOffset;
        transform.localRotation = Quaternion.Euler(0f, 0f, leverAngle) * _leverStartRotation;

        var pistonDistance = distance * pistonTravelRatio;
        if (piston != null)
        {
            piston.localPosition = _pistonStartPosition + Vector3.down * pistonDistance;
        }

        if (loadPoint != null)
        {
            loadPoint.localPosition = _loadPointStartPosition + Vector3.down * pistonDistance;
            pinballManager?.MoveLoadedBall(loadPoint.position);
        }

        if (spring != null)
        {
            var compression = PinballMotionMath.CalculateAnchoredCompression(
                _springOriginalHeight,
                pistonDistance);
            var scale = _springStartScale;
            scale.y = _springStartScale.y * compression.ScaleRatio;
            spring.localScale = scale;
            spring.localPosition = _springStartPosition +
                                   Vector3.up * compression.CenterOffset;
        }
    }

    private void StartSnap()
    {
        StopSnap();
        if (!isActiveAndEnabled)
        {
            ResetVisualsImmediate();
            return;
        }

        _snapCoroutine = StartCoroutine(SnapBack());
    }

    private IEnumerator SnapBack()
    {
        var leverPosition = transform.localPosition;
        var leverRotation = transform.localRotation;
        var pistonPosition = piston != null ? piston.localPosition : Vector3.zero;
        var loadPosition = loadPoint != null ? loadPoint.localPosition : Vector3.zero;
        var springPosition = spring != null ? spring.localPosition : Vector3.zero;
        var springScale = spring != null ? spring.localScale : Vector3.one;
        float elapsed = 0f;

        while (elapsed < snapDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / snapDuration);
            float eased = 1f - Mathf.Pow(1f - progress, 4f);
            transform.localPosition = Vector3.Lerp(leverPosition, _leverStartPosition, eased);
            transform.localRotation = Quaternion.Slerp(leverRotation, _leverStartRotation, eased);
            if (piston != null)
            {
                piston.localPosition = Vector3.Lerp(pistonPosition, _pistonStartPosition, eased);
            }
            if (loadPoint != null)
            {
                loadPoint.localPosition = Vector3.Lerp(loadPosition, _loadPointStartPosition, eased);
                pinballManager?.MoveLoadedBall(loadPoint.position);
            }
            if (spring != null)
            {
                spring.localPosition = Vector3.Lerp(springPosition, _springStartPosition, eased);
                spring.localScale = Vector3.Lerp(springScale, _springStartScale, eased);
            }
            glowController?.SetPullRatio(1f - eased);
            yield return null;
        }

        ResetVisualsImmediate();
        _snapCoroutine = null;
    }

    private void StopSnap()
    {
        if (_snapCoroutine == null) return;
        StopCoroutine(_snapCoroutine);
        _snapCoroutine = null;
    }

    private void ResetVisualsImmediate()
    {
        transform.localPosition = _leverStartPosition;
        transform.localRotation = _leverStartRotation;
        if (piston != null) piston.localPosition = _pistonStartPosition;
        if (loadPoint != null) loadPoint.localPosition = _loadPointStartPosition;
        if (spring != null)
        {
            spring.localPosition = _springStartPosition;
            spring.localScale = _springStartScale;
        }
        _pullDistance = 0f;
        _crossedMinimumPull = false;
        glowController?.SetPullRatio(0f);
    }
}
