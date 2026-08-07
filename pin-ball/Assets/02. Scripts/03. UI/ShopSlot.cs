using System;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ShopSlot : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private ShopTooltip tooltip;

    [SerializeField] private Color availableCostColor = Color.white;
    [SerializeField] private Color unavailableCostColor = Color.red;

    public Item Item { get; private set; }

    private Action<Item> _onPurchase;

    private void Awake()
    {
        purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
    }

    private void OnDisable()
    {
        tooltip?.Hide(this);
    }

    public void SetItem(Item item, Action<Item> onPurchase)
    {
        Item = item;
        _onPurchase = onPurchase;

        nameText.text = item?.Name ?? string.Empty;
        costText.text = item != null ? item.Cost.ToString() : string.Empty;

        iconImage.sprite = item?.Icon;
        iconImage.enabled = item?.Icon != null;
    }

    public void RefreshState(
        int currentGold,
        bool isPurchased,
        bool isPreparationPhase)
    {
        if (Item == null)
        {
            purchaseButton.interactable = false;
            costText.color = unavailableCostColor;
            return;
        }

        var canPurchase =
            isPreparationPhase &&
            !isPurchased &&
            currentGold >= Item.Cost;
        purchaseButton.interactable = canPurchase;
        costText.color = canPurchase ? availableCostColor : unavailableCostColor;
    }

    public void Clear()
    {
        Item = null;
        _onPurchase = null;

        if (nameText != null) nameText.text = string.Empty;
        if (costText != null)
        {
            costText.text = string.Empty;
            costText.color = availableCostColor;
        }

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        purchaseButton.interactable = false;
        tooltip.Hide(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Item == null) return;

        tooltip.Show(this, Item.Description, eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        tooltip.Move(this, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltip.Hide(this);
    }

    private void OnPurchaseButtonClicked()
    {
        if (Item == null) return;

        _onPurchase?.Invoke(Item);
    }

    private void OnDestroy()
    {
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveListener(OnPurchaseButtonClicked);
        }
    }
}
