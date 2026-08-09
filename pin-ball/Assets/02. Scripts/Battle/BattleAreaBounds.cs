using UnityEngine;

public class BattleAreaBounds : MonoBehaviour
{
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
