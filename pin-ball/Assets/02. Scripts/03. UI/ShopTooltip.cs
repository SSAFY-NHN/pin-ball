using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ShopTooltip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Vector2 cursorOffset = new(75f, 50f);

    private ShopSlot _owner;
    
    private Canvas _canvas;
    private RectTransform _tooltipRect;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _tooltipRect = transform as RectTransform;

        var canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    public void Show(ShopSlot owner, string description, Vector2 screenPosition)
    {
        _owner = owner;
        descriptionText.text = description;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipRect);
        SetPosition(screenPosition);
    }

    public void Move(ShopSlot owner, Vector2 screenPosition)
    {
        if (_owner != owner || !gameObject.activeSelf) return;
        
        SetPosition(screenPosition);
    }

    public void Hide(ShopSlot owner)
    {
        if (_owner != owner) return;

        _owner = null;
        gameObject.SetActive(false);
    }

    private void SetPosition(Vector2 screenPosition)
    {
        var canvasRect = _canvas.transform as RectTransform;
        var eventCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition + cursorOffset,
                eventCamera,
                out var localPosition))
        {
            return;
        }

        var canvasBounds = canvasRect.rect;
        var tooltipBounds = _tooltipRect.rect;
        var pivot = _tooltipRect.pivot;

        var left = tooltipBounds.width * pivot.x;
        var right = tooltipBounds.width * (1f - pivot.x);
        var bottom = tooltipBounds.height * pivot.y;
        var top = tooltipBounds.height * (1f - pivot.y);

        localPosition.x = Mathf.Clamp(
            localPosition.x,
            canvasBounds.xMin + left,
            canvasBounds.xMax - right);
        localPosition.y = Mathf.Clamp(
            localPosition.y,
            canvasBounds.yMin + bottom,
            canvasBounds.yMax - top);

        _tooltipRect.position = canvasRect.TransformPoint(localPosition);
    }
}