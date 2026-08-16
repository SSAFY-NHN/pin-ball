using System;
using System.Collections.Generic;

using UnityEngine;

internal sealed class ItemEventController
{
    private readonly Dictionary<EItem, List<IItemEventListener>> _subscribers = new();
    private readonly Queue<EItem> _eventQueue = new();

    private bool _isDispatching;

    public bool Subscribe(EItem item, IItemEventListener listener)
    {
        if (IsNull(listener))
        {
            Debug.LogError("아이템 이벤트 리스너가 null입니다.");
            return false;
        }

        if (!_subscribers.TryGetValue(item, out var listeners))
        {
            listeners = new List<IItemEventListener>();
            _subscribers.Add(item, listeners);
        }

        if (listeners.Contains(listener)) return false;

        listeners.Add(listener);
        return true;
    }

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

    public void Enqueue(EItem item)
    {
        _eventQueue.Enqueue(item);
    }

    public void DispatchQueued(ItemCatalogController catalog)
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
                Dispatch(eventCode, catalog);
            }
        }
        finally
        {
            _isDispatching = false;
        }
    }

    public void DispatchImmediate(EItem item, ItemCatalogController catalog)
    {
        Dispatch(item, catalog);
    }

    public void Clear()
    {
        _subscribers.Clear();
        _eventQueue.Clear();
    }

    private void Dispatch(EItem item, ItemCatalogController catalog)
    {
        catalog.EnsureInitialized();

        if (!catalog.TryGetItem(item, out var itemData))
        {
            Debug.LogError($"아이템 데이터를 찾을 수 없습니다: {item}");
            return;
        }

        if (!_subscribers.TryGetValue(item, out var listeners))
        {
            return;
        }

        var snapshot = listeners.ToArray();

        foreach (var listener in snapshot)
        {
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

    private static bool IsNull(IItemEventListener listener)
    {
        return listener == null ||
               // ReSharper disable once SuspiciousTypeConversion.Global
               listener is UnityEngine.Object unityObject && unityObject == null;
    }
}
