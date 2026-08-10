using UnityEngine;

public class BattleAreaBounds : MonoBehaviour
{
    private const float AllyGridGap = 0.15f;

    public bool IsValid { get; private set; }

    [SerializeField] private RectTransform battleArea;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Camera worldCamera;

    private readonly Vector3[] _corners = new Vector3[4];
    private Vector2 _worldMin;
    private Vector2 _worldMax;
    private int _screenWidth;
    private int _screenHeight;

    private void Awake()
    {
        ValidateReferences();
        RefreshBounds();
    }

    private void Update()
    {
        if (_screenWidth == Screen.width &&
            _screenHeight == Screen.height) return;

        RefreshBounds();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!Application.isPlaying) return;
        RefreshBounds();
    }

    public bool Contains(Vector3 worldPosition, float padding)
    {
        if (!IsValid) return false;

        float safePadding = Mathf.Max(0f, padding);
        return worldPosition.x >= _worldMin.x + safePadding &&
               worldPosition.x <= _worldMax.x - safePadding &&
               worldPosition.y >= _worldMin.y + safePadding &&
               worldPosition.y <= _worldMax.y - safePadding;
    }

    public Vector3 Clamp(Vector3 worldPosition, float padding)
    {
        if (!IsValid) return worldPosition;

        float safePadding = Mathf.Max(0f, padding);
        worldPosition.x = Mathf.Clamp(
            worldPosition.x,
            _worldMin.x + safePadding,
            _worldMax.x - safePadding);
        worldPosition.y = Mathf.Clamp(
            worldPosition.y,
            _worldMin.y + safePadding,
            _worldMax.y - safePadding);
        return worldPosition;
    }

    public bool ContainsAllyPlacement(
        Vector3 worldPosition,
        float padding)
    {
        return IsValid && ContainsAllyPlacement(
            _worldMin,
            _worldMax,
            worldPosition,
            padding);
    }

    public Vector3 ClampAllyPlacement(
        Vector3 worldPosition,
        float padding)
    {
        return IsValid
            ? ClampAllyPlacement(
                _worldMin,
                _worldMax,
                worldPosition,
                padding)
            : worldPosition;
    }

    public bool TryGetAllyGridPosition(
        int gridIndex,
        float padding,
        out Vector3 position)
    {
        if (!IsValid)
        {
            position = default;
            return false;
        }

        return TryGetAllyGridPosition(
            _worldMin,
            _worldMax,
            gridIndex,
            padding,
            out position);
    }

    private static bool ContainsAllyPlacement(
        Vector2 worldMin,
        Vector2 worldMax,
        Vector3 worldPosition,
        float padding)
    {
        float safePadding = Mathf.Max(0f, padding);
        float midpoint = (worldMin.x + worldMax.x) * 0.5f;
        return worldPosition.x >= midpoint + safePadding &&
               worldPosition.x <= worldMax.x - safePadding &&
               worldPosition.y >= worldMin.y + safePadding &&
               worldPosition.y <= worldMax.y - safePadding;
    }

    private static Vector3 ClampAllyPlacement(
        Vector2 worldMin,
        Vector2 worldMax,
        Vector3 worldPosition,
        float padding)
    {
        float safePadding = Mathf.Max(0f, padding);
        float midpoint = (worldMin.x + worldMax.x) * 0.5f;
        worldPosition.x = Mathf.Clamp(
            worldPosition.x,
            midpoint + safePadding,
            worldMax.x - safePadding);
        worldPosition.y = Mathf.Clamp(
            worldPosition.y,
            worldMin.y + safePadding,
            worldMax.y - safePadding);
        return worldPosition;
    }

    private static bool TryGetAllyGridPosition(
        Vector2 worldMin,
        Vector2 worldMax,
        int gridIndex,
        float padding,
        out Vector3 position)
    {
        position = default;
        if (gridIndex < 0) return false;

        float safePadding = Mathf.Max(0f, padding);
        float midpoint = (worldMin.x + worldMax.x) * 0.5f;
        float minX = midpoint + safePadding;
        float maxX = worldMax.x - safePadding;
        float minY = worldMin.y + safePadding;
        float maxY = worldMax.y - safePadding;
        float step = Mathf.Max(
            safePadding * 2f + AllyGridGap,
            AllyGridGap);
        int columnCount = Mathf.FloorToInt((maxX - minX) / step) + 1;
        if (columnCount <= 0 || minX > maxX || minY > maxY) return false;

        int column = gridIndex % columnCount;
        int row = gridIndex / columnCount;
        float y = maxY - row * step;
        if (y < minY) return false;

        position = new Vector3(minX + column * step, y, 0f);
        return true;
    }

    private void ValidateReferences()
    {
        IsValid = true;

        if (battleArea == null)
        {
            Debug.LogError("[BattleAreaBounds] Missing reference: battleArea");
            IsValid = false;
        }

        if (canvas == null)
        {
            Debug.LogError("[BattleAreaBounds] Missing reference: canvas");
            IsValid = false;
        }

        if (worldCamera == null)
        {
            Debug.LogError("[BattleAreaBounds] Missing reference: worldCamera");
            IsValid = false;
        }
    }

    private void RefreshBounds()
    {
        _screenWidth = Screen.width;
        _screenHeight = Screen.height;

        if (battleArea == null || canvas == null || worldCamera == null)
        {
            IsValid = false;
            return;
        }

        battleArea.GetWorldCorners(_corners);
        Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;
        Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(
            canvasCamera,
            _corners[0]);
        Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(
            canvasCamera,
            _corners[2]);
        float worldDepth = Mathf.Abs(worldCamera.transform.position.z);
        Vector3 min = worldCamera.ScreenToWorldPoint(
            new Vector3(screenMin.x, screenMin.y, worldDepth));
        Vector3 max = worldCamera.ScreenToWorldPoint(
            new Vector3(screenMax.x, screenMax.y, worldDepth));

        _worldMin = Vector2.Min(min, max);
        _worldMax = Vector2.Max(min, max);
        IsValid = true;
    }
}
