public static class FullscreenRenderScalePolicy
{
    private const int QhdWidth = 2560;
    private const int QhdHeight = 1440;
    private const float FullResolutionScale = 1f;
    private const float QhdFullscreenScale = 0.85f;

    public static int TargetFrameRate => 60;

    public static float Resolve(int width, int height, bool isFullScreen)
    {
        if (width <= 0 || height <= 0 || !isFullScreen)
        {
            return FullResolutionScale;
        }

        return width >= QhdWidth && height >= QhdHeight
            ? QhdFullscreenScale
            : FullResolutionScale;
    }
}
