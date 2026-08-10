using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialFocusIndicator : MonoBehaviour
{
    [SerializeField] private RectTransform frame;
    [SerializeField] private RectTransform arrow;
    [SerializeField] private Image inputBlocker;
    [SerializeField] private Vector2 worldTargetSize = new(120f, 120f);

    private RectTransform _canvasRect;
    private Canvas _canvas;
    private Transform _target;
    private Vector2 _padding;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _canvasRect = _canvas != null ? _canvas.transform as RectTransform : null;
        Hide();
    }

    public void Focus(Transform target, Vector2 padding)
    {
        _target = target;
        _padding = padding;
        gameObject.SetActive(target != null);
        UpdatePosition();
    }

    public void Hide()
    {
        _target = null;
        gameObject.SetActive(false);
    }

    public void SetInputBlocked(bool blocked)
    {
        if (inputBlocker == null) return;
        inputBlocker.enabled = blocked;
        inputBlocker.raycastTarget = blocked;
    }

    private void LateUpdate()
    {
        UpdatePosition();
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.05f;
        if (frame != null) frame.localScale = Vector3.one * pulse;
        if (arrow != null)
        {
            var position = arrow.anchoredPosition;
            position.y = -18f + Mathf.Sin(Time.unscaledTime * 6f) * 7f;
            arrow.anchoredPosition = position;
        }
    }

    private void UpdatePosition()
    {
        if (_target == null || frame == null || _canvasRect == null) return;

        Camera camera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _canvas.worldCamera
            : null;

        if (_target is RectTransform targetRect)
        {
            var corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, min, camera, out min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, max, camera, out max);
            frame.anchoredPosition = (min + max) * 0.5f;
            frame.sizeDelta = max - min + _padding;
            return;
        }

        Camera worldCamera = Camera.main;
        if (worldCamera == null) return;
        Vector2 screenPoint = worldCamera.WorldToScreenPoint(_target.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            screenPoint,
            camera,
            out Vector2 localPoint);
        frame.anchoredPosition = localPoint;
        frame.sizeDelta = worldTargetSize + _padding;
    }
}
