using UnityEngine;

public abstract class AppService : MonoBehaviour
{
    protected virtual void Awake()
    {
        App.Register(this);
    }

    protected virtual void OnDestroy()
    {
        App.Unregister(this);
    }
}