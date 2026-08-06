using System;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemSlot : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private Button purchaseButton;

    public Item Item { get; private set; }

    private Action<Item> _onPurchase;

    private void Awake()
    {
        purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
    }

    private void OnDestroy()
    {
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveListener(OnPurchaseButtonClicked);
        }
    }

    public void SetItem(Item item, Action<Item> onPurchase)
    {
        Item = item;
        _onPurchase = onPurchase;

        nameText.text = item?.Name ?? string.Empty;
        descriptionText.text = item?.Description ?? string.Empty;
        costText.text = item != null ? item.Cost.ToString() : string.Empty;

        if (iconImage != null)
        {
            iconImage.sprite = item?.Icon;
            iconImage.enabled = item?.Icon != null;
        }
    }

    public void RefreshState(int currentGold, bool isPurchased)
    {
        if (Item == null)
        {
            purchaseButton.interactable = false;
            SetStateText(string.Empty);
            return;
        }

        purchaseButton.interactable =
            !isPurchased && currentGold >= Item.Cost;

        if (isPurchased)
        {
            SetStateText("구매 완료");
        }
        else if (currentGold < Item.Cost)
        {
            SetStateText("골드 부족");
        }
        else
        {
            SetStateText(string.Empty);
        }
    }

    public void Clear()
    {
        Item = null;
        _onPurchase = null;

        if (nameText != null) nameText.text = string.Empty;
        if (descriptionText != null) descriptionText.text = string.Empty;
        if (costText != null) costText.text = string.Empty;
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        purchaseButton.interactable = false;
        SetStateText(string.Empty);
    }

    private void OnPurchaseButtonClicked()
    {
        if (Item == null) return;

        _onPurchase?.Invoke(Item);
    }

    private void SetStateText(string value)
    {
        if (stateText != null)
        {
            stateText.text = value;
        }
    }
}
