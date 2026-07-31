using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class App : Singleton<App>
{
    private static readonly Dictionary<Type, MonoBehaviour> _services = new();

    protected override void Awake()
    {
        base.Awake();

        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 120;

        DOTween.safeModeLogBehaviour = DG.Tweening.Core.Enums.SafeModeLogBehaviour.Error;
    }
    
    internal static void Register(MonoBehaviour service)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        Type type = service.GetType();

        if (_services.TryGetValue(type, out MonoBehaviour existing))
        {
            if (existing != null && existing != service)
            {
                throw new InvalidOperationException(
                    $"{type.Name} is already registered.\n" +
                    $"Existing: {existing.name}\n" +
                    $"New: {service.name}");
            }
        }

        _services[type] = service;
    }

    internal static void Unregister(MonoBehaviour service)
    {
        if (service == null)
        {
            return;
        }

        Type type = service.GetType();

        if (_services.TryGetValue(type, out MonoBehaviour registered) &&
            ReferenceEquals(registered, service))
        {
            _services.Remove(type);
        }
    }

    public static T Get<T>() where T : MonoBehaviour
    {
        Type type = typeof(T);

        if (_services.TryGetValue(type, out MonoBehaviour service) &&
            service is T result &&
            result != null)
        {
            return result;
        }

        _services.Remove(type);

        throw new InvalidOperationException(
            $"{type.Name} is not registered.");
    }

    public static bool TryGet<T>(out T result) where T : MonoBehaviour
    {
        Type type = typeof(T);

        if (_services.TryGetValue(type, out MonoBehaviour service) &&
            service is T typedService &&
            typedService != null)
        {
            result = typedService;
            return true;
        }

        _services.Remove(type);

        result = null;
        return false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry()
    {
        _services.Clear();
    }
}