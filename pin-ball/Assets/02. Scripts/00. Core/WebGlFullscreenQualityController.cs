using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class WebGlFullscreenQualityController : MonoBehaviour
{
    [SerializeField] private UniversalRenderPipelineAsset pipelineAsset;

    private float _originalRenderScale;
    private bool _hasOriginalRenderScale;
    private int _lastScreenWidth = -1;
    private int _lastScreenHeight = -1;
    private bool _lastFullScreen;

    private void OnEnable()
    {
        CaptureOriginalRenderScale();
        ApplyCurrentScreenInWebGl();
    }

    private void Update()
    {
        ApplyCurrentScreenInWebGl();
    }

    public void Configure(UniversalRenderPipelineAsset asset)
    {
        RestoreOriginalRenderScale();
        pipelineAsset = asset;
        _hasOriginalRenderScale = false;
        InvalidateScreenState();
        CaptureOriginalRenderScale();
    }

    public bool ApplyIfChanged(int width, int height, bool isFullScreen)
    {
        if (pipelineAsset == null) return false;

        float targetScale = FullscreenRenderScalePolicy.Resolve(
            width,
            height,
            isFullScreen);
        bool screenUnchanged =
            width == _lastScreenWidth &&
            height == _lastScreenHeight &&
            isFullScreen == _lastFullScreen;
        if (screenUnchanged &&
            Mathf.Approximately(pipelineAsset.renderScale, targetScale))
        {
            return false;
        }

        _lastScreenWidth = width;
        _lastScreenHeight = height;
        _lastFullScreen = isFullScreen;

        if (Mathf.Approximately(pipelineAsset.renderScale, targetScale))
        {
            return false;
        }

        pipelineAsset.renderScale = targetScale;
        return true;
    }

    private void CaptureOriginalRenderScale()
    {
        if (_hasOriginalRenderScale || pipelineAsset == null) return;

        _originalRenderScale = pipelineAsset.renderScale;
        _hasOriginalRenderScale = true;
    }

    public void RestoreOriginalRenderScale()
    {
        if (!_hasOriginalRenderScale || pipelineAsset == null) return;

        pipelineAsset.renderScale = _originalRenderScale;
    }

    private void InvalidateScreenState()
    {
        _lastScreenWidth = -1;
        _lastScreenHeight = -1;
        _lastFullScreen = false;
    }

    private void ApplyCurrentScreenInWebGl()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        ApplyIfChanged(Screen.width, Screen.height, Screen.fullScreen);
#endif
    }

    private void OnDisable()
    {
        RestoreOriginalRenderScale();
    }

    private void OnDestroy()
    {
        RestoreOriginalRenderScale();
    }
}
