using System.Collections;

using UnityEngine;
using UnityEngine.UI;

public enum BottomPanelTab
{
    Items,
    Shop
}

public class BottomTabPanel : UIBase
{
    public override bool IsDefaultPanel => true;

    [SerializeField] private Button itemsButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private GameObject itemsContent;
    [SerializeField] private GameObject shopContent;

    private BottomPanelTab _lastTab = BottomPanelTab.Items;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);

        if (!ValidateReferences()) return;

        itemsButton.onClick.AddListener(ShowItems);
        shopButton.onClick.AddListener(ShowShop);
        StartCoroutine(ApplyInitialTabNextFrame());
    }

    public void ShowShop()
    {
        _lastTab = BottomPanelTab.Shop;
        ApplyTab();
    }

    public void ShowItems()
    {
        _lastTab = BottomPanelTab.Items;
        ApplyTab();
    }

    private IEnumerator ApplyInitialTabNextFrame()
    {
        yield return null;
        ApplyTab();
    }

    private void ApplyTab()
    {
        if (itemsContent == null || shopContent == null) return;

        bool showItems = _lastTab == BottomPanelTab.Items;
        itemsContent.SetActive(showItems);
        shopContent.SetActive(!showItems);
        itemsButton.interactable = !showItems;
        shopButton.interactable = showItems;
    }

    private bool ValidateReferences()
    {
        bool isValid = true;
        isValid &= ValidateReference(itemsButton, nameof(itemsButton));
        isValid &= ValidateReference(shopButton, nameof(shopButton));
        isValid &= ValidateReference(itemsContent, nameof(itemsContent));
        isValid &= ValidateReference(shopContent, nameof(shopContent));
        return isValid;
    }

    private bool ValidateReference(Object reference, string fieldName)
    {
        if (reference != null) return true;
        Debug.LogError($"[BottomTabPanel] Missing reference: {fieldName}");
        return false;
    }

    private void OnDestroy()
    {
        if (itemsButton != null)
        {
            itemsButton.onClick.RemoveListener(ShowItems);
        }

        if (shopButton != null)
        {
            shopButton.onClick.RemoveListener(ShowShop);
        }
    }
}
