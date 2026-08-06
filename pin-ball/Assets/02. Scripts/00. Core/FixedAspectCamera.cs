using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class FixedAspectCamera : MonoBehaviour
{
    private const float TargetAspect = 16f / 9f;

    private Camera _camera;
    private int _lastScreenWidth;
    private int _lastScreenHeight;

    private void OnEnable()
    {
        _camera = GetComponent<Camera>();
        ApplyAspectRatio();
    }

    private void LateUpdate()
    {
        if (_lastScreenWidth != Screen.width || _lastScreenHeight != Screen.height)
        {
            ApplyAspectRatio();
        }
    }

    private void ApplyAspectRatio()
    {
        if (_camera == null || Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;

        var windowAspect = Screen.width / (float)Screen.height;
        var scaleHeight = windowAspect / TargetAspect;

        if (scaleHeight < 1f)
        {
            _camera.rect = new Rect(0f, (1f - scaleHeight) * 0.5f, 1f, scaleHeight);
        }
        else
        {
            var scaleWidth = 1f / scaleHeight;
            _camera.rect = new Rect((1f - scaleWidth) * 0.5f, 0f, scaleWidth, 1f);
        }

        _camera.ResetAspect();
    }
}
