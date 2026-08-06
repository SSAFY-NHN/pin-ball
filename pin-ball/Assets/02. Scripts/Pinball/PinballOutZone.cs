using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PinballOutZone : MonoBehaviour
{
    private PinballManager _pinballManager;

    private void Start()
    {
        _pinballManager = App.Get<PinballManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var ball = other.GetComponent<Pinball>();
        if (ball == null) return;

        _pinballManager.ReleaseBall(ball);
    }
}