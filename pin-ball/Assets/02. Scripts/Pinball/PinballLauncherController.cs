using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PinballLauncherController : MonoBehaviour
{
    [SerializeField] private PinballManager pinballManager;
    [SerializeField] private Transform loadPoint;
    [SerializeField] private Transform piston;
    [SerializeField] private Transform spring;
    [SerializeField] private Vector2 launchDirection = Vector2.up;
    [SerializeField, Min(0.1f)] private float maximumPullDistance = 1.1f;
    [SerializeField, Range(0f, 1f)] private float minimumPullRatio = 0.1f;
    [SerializeField, Range(0f, 60f)] private float leverMaximumAngle = 28f;
    [SerializeField, Range(0f, 1f)] private float pistonTravelRatio = 0.65f;

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
    private bool _hasLoadedBall;
    private float _leverRotationDirection;

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
        if (!_hasLoadedBall) return;
        _isDragging = true;
        _pointerStartY = GetPointerWorldY();
        _pullDistance = 0f;
    }

    private void OnMouseDrag()
    {
        if (!_isDragging) return;

        _pullDistance = Mathf.Clamp(
            _pointerStartY - GetPointerWorldY(),
            0f,
            maximumPullDistance);
        ApplyVisualPull(_pullDistance);
    }

    private void OnMouseUp()
    {
        if (!_isDragging) return;
        _isDragging = false;

        var pullRatio = _pullDistance / maximumPullDistance;
        var launched = pullRatio >= minimumPullRatio &&
                       pinballManager != null &&
                       pinballManager.TryLaunchLoadedBall(pullRatio);
        if (launched)
        {
            _hasLoadedBall = false;
        }

        ResetVisuals();
    }

    private void OnDisable()
    {
        _isDragging = false;
        ResetVisuals();
    }

    public void SetLoaded(bool isLoaded)
    {
        _hasLoadedBall = isLoaded;
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

    private void ResetVisuals()
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
    }
}
