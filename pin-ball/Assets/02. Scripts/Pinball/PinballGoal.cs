using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PinballGoal : MonoBehaviour
{
    private PinballManager _pinballManager;
    private BoxCollider2D _collider;
    private float _baseWidth;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _collider.isTrigger = true;
        _baseWidth = _collider.size.x;
    }

    private void Start()
    {
        _pinballManager = App.Get<PinballManager>();
        _pinballManager.RegisterGoal(this);
    }

    private void OnDestroy()
    {
        _pinballManager?.UnregisterGoal(this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var ball = other.GetComponentInParent<Pinball>();
        if (ball == null) return;

        _pinballManager.OnGoalBall(ball);
    }

    internal void SetWidthMultiplier(float multiplier)
    {
        var size = _collider.size;
        size.x = _baseWidth * Mathf.Max(0.1f, multiplier);
        _collider.size = size;
    }
}
