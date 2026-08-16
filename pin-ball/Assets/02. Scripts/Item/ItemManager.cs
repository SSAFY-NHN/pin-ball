using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public interface IItemEventListener
{
    void OnItemEvent(Item item);
}

public class ItemManager : AppService
{
    public event Action<Item> OnItemPurchased;
    public event Action<Item> OnItemConsumed;
    public event Action<Item> OnItemAcquired;

    private readonly ItemCatalogController _catalog = new();
    private readonly ItemInventoryController _inventory = new();
    private readonly ItemEventController _events = new();
    private ItemPurchaseController _purchaseController;

    private Coroutine _queuedEventCoroutine;

    private void Start()
    {
        _catalog.EnsureInitialized();
        _purchaseController = new ItemPurchaseController(_inventory);
    }

    /// <summary>특정 아이템 이벤트를 구독한다.</summary>
    public void Subscribe(EItem item, IItemEventListener listener)
    {
        var isNewListener = _events.Subscribe(item, listener);

        if (isNewListener &&
            _inventory.HasItem(item) &&
            _catalog.TryGetItem(item, out var activeItem))
        {
            listener.OnItemEvent(activeItem);
        }
    }

    /// <summary>특정 아이템 이벤트의 구독을 해제한다.</summary>
    public void Unsubscribe(EItem item, IItemEventListener listener)
    {
        _events.Unsubscribe(item, listener);
    }

    /// <summary>
    /// 아이템 이벤트를 큐에 넣는다.
    /// 실제 전달은 Update()에서 이루어진다.
    /// </summary>
    public void Raise(EItem item)
    {
        NotifyItemAcquired(item);
        _events.Enqueue(item);
        ScheduleQueuedEvents();
    }

    /// <summary>아이템 이벤트를 큐를 거치지 않고 즉시 전달한다.</summary>
    public void RaiseImmediate(EItem item)
    {
        NotifyItemAcquired(item);
        _events.DispatchImmediate(item, _catalog);
    }
    
    private void NotifyItemAcquired(EItem item)
    {
        _catalog.EnsureInitialized();
        _inventory.Acquire(item);

        if (_catalog.TryGetItem(item, out var itemData))
        {
            OnItemAcquired?.Invoke(itemData);
        }
    }

    /// <summary>지정한 초가 지난 뒤 아이템 이벤트를 큐에 넣는다.</summary>
    public void RaiseDelayed(EItem item, float delaySeconds)
    {
        if (delaySeconds <= 0f)
        {
            Raise(item);
            return;
        }

        StartCoroutine(RaiseAfterDelay(item, delaySeconds));
    }

    /// <summary>현재 큐에 들어 있는 아이템 이벤트를 모두 처리한다.</summary>
    public void DispatchQueuedEvents()
    {
        if (_queuedEventCoroutine != null)
        {
            StopCoroutine(_queuedEventCoroutine);
            _queuedEventCoroutine = null;
        }

        _events.DispatchQueued(_catalog);
    }

    /// <summary>모든 구독과 대기 중인 아이템 이벤트를 제거한다.</summary>
    public void Clear()
    {
        ResetRunState();
        _events.ClearSubscribers();
    }

    /// <summary>구독을 유지하고 현재 런의 아이템 상태만 초기화한다.</summary>
    public void ResetRunState()
    {
        StopAllCoroutines();
        _queuedEventCoroutine = null;
        _events.ClearQueuedEvents();
        _inventory.Clear();
    }

    public bool HasItem(EItem item)
    {
        return _inventory.HasItem(item);
    }

    public int GetItemCount(EItem item) =>
        _inventory.GetItemCount(item);

    public int GetPurchaseLimit(EItem item) =>
        _inventory.GetPurchaseLimit(item);

    public bool CanPurchase(EItem item) =>
        _inventory.CanPurchase(item);

    public bool TryConsume(EItem item)
    {
        _catalog.EnsureInitialized();
        if (!_inventory.TryConsume(item)) return false;

        if (_catalog.TryGetItem(item, out var consumedItem))
        {
            OnItemConsumed?.Invoke(consumedItem);
        }
        return true;
    }

    public bool TryPurchase(Item item)
    {
        _purchaseController ??= new ItemPurchaseController(_inventory);
        return _purchaseController.TryPurchase(
            item,
            Raise,
            NotifyItemPurchased);
    }

    public bool TryGetItem(EItem item, out Item result)
    {
        _catalog.EnsureInitialized();
        return _catalog.TryGetItem(item, out result);
    }

    public void GetItems(List<Item> result)
    {
        if (result == null) return;

        _catalog.EnsureInitialized();
        _catalog.GetItems(result);
    }

    public void GetActiveItems(List<Item> result)
    {
        if (result == null) return;

        _catalog.EnsureInitialized();
        _catalog.GetActiveItems(result, _inventory);
    }

    private IEnumerator RaiseAfterDelay(EItem item, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        Raise(item);
    }

    private void ScheduleQueuedEvents()
    {
        if (_queuedEventCoroutine != null) return;

        _queuedEventCoroutine = StartCoroutine(
            DispatchQueuedEventsNextFrame());
    }

    private IEnumerator DispatchQueuedEventsNextFrame()
    {
        yield return null;
        _events.DispatchQueued(_catalog);
        _queuedEventCoroutine = null;
    }

    private void NotifyItemPurchased(Item item)
    {
        OnItemPurchased?.Invoke(item);
    }
}
