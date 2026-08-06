using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ShopItemTooltip : MonoBehaviour
{
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Vector2 cursorOffset = new(18f, -18f);

    private ShopItemSlot _owner;

    private void Awake()
    {
        if (tooltipRect == null)
        {
            tooltipRect = transform as RectTransform;
        }

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        var canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    public void Show(ShopItemSlot owner, string description, Vector2 screenPosition)
    {
        if (owner == null || tooltipRect == null || descriptionText == null || canvas == null)
        {
            return;
        }

        _owner = owner;
        descriptionText.text = description;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
        SetPosition(screenPosition);
    }

    public void Move(ShopItemSlot owner, Vector2 screenPosition)
    {
        if (_owner != owner || !gameObject.activeSelf)
        {
            return;
        }

        SetPosition(screenPosition);
    }

    public void Hide(ShopItemSlot owner)
    {
        if (_owner != owner)
        {
            return;
        }

        _owner = null;
        gameObject.SetActive(false);
    }

    private void SetPosition(Vector2 screenPosition)
    {
        var canvasRect = canvas.transform as RectTransform;
        var eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition + cursorOffset,
                eventCamera,
                out var localPosition))
        {
            return;
        }

        var canvasBounds = canvasRect.rect;
        var tooltipBounds = tooltipRect.rect;
        var pivot = tooltipRect.pivot;

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

        tooltipRect.position = canvasRect.TransformPoint(localPosition);
    }
}