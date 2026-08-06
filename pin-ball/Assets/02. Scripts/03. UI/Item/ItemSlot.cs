using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private ItemTooltip tooltip;

    public Item Item { get; private set; }

    private void OnDisable()
    {
        tooltip?.Hide(this);
    }

    public void SetItem(Item item)
    {
        Item = item;

        if (iconImage == null) return;

        iconImage.sprite = item?.Icon;
        iconImage.enabled = item?.Icon != null;
    }

    public void Clear()
    {
        Item = null;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        tooltip?.Hide(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Item == null) return;

        tooltip?.Show(
            this,
            Item.Name,
            Item.Description,
            eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        tooltip?.Move(this, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltip?.Hide(this);
    }
}