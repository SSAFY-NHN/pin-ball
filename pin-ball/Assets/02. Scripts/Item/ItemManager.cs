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
    public event Action<Item> OnItemAcquired;

    private readonly Dictionary<EItem, Item> _items = new();
    private readonly HashSet<EItem> _activeItems = new();
    private readonly Dictionary<EItem, List<IItemEventListener>> _subscribers = new();
    private readonly Queue<EItem> _eventQueue = new();

    private bool _isDispatching;
    private bool _isInitialized;

    private void Start()
    {
        InitializeItems();
    }

    private void InitializeItems()
    {
        if (_isInitialized) return;

        var titleData = App.Get<TitleData>();
        foreach (var data in titleData.Item)
        {
            var item = new Item(data.Value, null);
            _items.Add(item.Key, item);
        }

        _isInitialized = true;
    }

    private void Update()
    {
        DispatchQueuedEvents();
    }

    /// <summary>특정 아이템 이벤트를 구독한다.</summary>
    public void Subscribe(EItem item, IItemEventListener listener)
    {
        if (IsNull(listener))
        {
            Debug.LogError("아이템 이벤트 리스너가 null입니다.");
            return;
        }

        if (!_subscribers.TryGetValue(item, out var listeners))
        {
            listeners = new List<IItemEventListener>();
            _subscribers.Add(item, listeners);
        }

        var isNewListener = !listeners.Contains(listener);
        if (isNewListener)
        {
            listeners.Add(listener);
        }

        if (isNewListener &&
            _activeItems.Contains(item) &&
            _items.TryGetValue(item, out var activeItem))
        {
            listener.OnItemEvent(activeItem);
        }
    }

    /// <summary>특정 아이템 이벤트의 구독을 해제한다.</summary>
    public void Unsubscribe(EItem item, IItemEventListener listener)
    {
        if (!_subscribers.TryGetValue(item, out var listeners))
        {
            return;
        }

        listeners.Remove(listener);

        if (listeners.Count == 0)
        {
            _subscribers.Remove(item);
        }
    }

    /// <summary>
    /// 아이템 이벤트를 큐에 넣는다.
    /// 실제 전달은 Update()에서 이루어진다.
    /// </summary>
    public void Raise(EItem item)
    {
        NotifyItemAcquired(item);
        _eventQueue.Enqueue(item);
    }

    /// <summary>아이템 이벤트를 큐를 거치지 않고 즉시 전달한다.</summary>
    public void RaiseImmediate(EItem item)
    {
        NotifyItemAcquired(item);
        Dispatch(item);
    }
    
    private void NotifyItemAcquired(EItem item)
    {
        InitializeItems();

        if (!_activeItems.Add(item)) return;

        if (_items.TryGetValue(item, out var itemData))
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
        if (_isDispatching)
        {
            return;
        }

        _isDispatching = true;

        try
        {
            while (_eventQueue.Count > 0)
            {
                var eventCode = _eventQueue.Dequeue();
                Dispatch(eventCode);
            }
        }
        finally
        {
            _isDispatching = false;
        }
    }

    /// <summary>모든 구독과 대기 중인 아이템 이벤트를 제거한다.</summary>
    public void Clear()
    {
        _subscribers.Clear();
        _eventQueue.Clear();
        _activeItems.Clear();
        StopAllCoroutines();
    }

    public bool HasItem(EItem item)
    {
        return _activeItems.Contains(item);
    }

    public bool TryPurchase(Item item)
    {
        if (item == null || HasItem(item.Key)) return false;

        var battleManager = App.Get<BattleManager>();
        if (!battleManager.TrySpendPreparationGold(item.Cost)) return false;

        Raise(item.Key);
        return true;
    }

    public bool TryGetItem(EItem item, out Item result)
    {
        InitializeItems();
        return _items.TryGetValue(item, out result);
    }

    public void GetItems(List<Item> result)
    {
        if (result == null) return;

        InitializeItems();
        result.Clear();
        result.AddRange(_items.Values);
    }

    public void GetActiveItems(List<Item> result)
    {
        if (result == null) return;

        InitializeItems();
        result.Clear();

        foreach (var item in _items.Values)
        {
            if (_activeItems.Contains(item.Key))
            {
                result.Add(item);
            }
        }
    }

    private void Dispatch(EItem item)
    {
        InitializeItems();

        if (!_items.TryGetValue(item, out var itemData))
        {
            Debug.LogError($"아이템 데이터를 찾을 수 없습니다: {item}");
            return;
        }

        if (!_subscribers.TryGetValue(item, out var listeners))
        {
            return;
        }

        // 콜백 안에서 구독/해제해도 현재 순회가 깨지지 않게 복사한다.
        var snapshot = listeners.ToArray();

        foreach (var listener in snapshot)
        {
            // 인터페이스 참조로 들고 있는 파괴된 MonoBehaviour도 걸러낸다.
            if (IsNull(listener))
            {
                listeners.Remove(listener);
                continue;
            }

            try
            {
                listener.OnItemEvent(itemData);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        if (listeners.Count == 0)
        {
            _subscribers.Remove(item);
        }
    }

    private bool IsNull(IItemEventListener listener)
    {
        return listener == null ||
               // ReSharper disable once SuspiciousTypeConversion.Global
               listener is UnityEngine.Object unityObject && unityObject == null;
    }

    private IEnumerator RaiseAfterDelay(EItem item, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        Raise(item);
    }
}
