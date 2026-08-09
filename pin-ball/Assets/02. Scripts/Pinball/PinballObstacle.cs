using UnityEngine;

public enum EPinballObstacle
{
    SmallPin,
    BigBumper
}

public class PinballObstacle : MonoBehaviour
{
    [SerializeField] private EPinballObstacle type;
    [SerializeField, Min(0f)] private float bumperMinimumExitSpeed = 6f;
    [SerializeField, Min(0f)] private float bumperSpeedBonus = 1f;

    public EPinballObstacle Type => type;
    private ArcaneMaskGlowController glow;

    private void Awake()
    {
        if (type != EPinballObstacle.BigBumper) return;
        var catalog = ArcaneVfxCatalog.Load();
        var renderer = GetComponent<SpriteRenderer>();
        if (catalog != null && renderer != null)
        {
            glow = ArcaneMaskGlowController.Attach(renderer, catalog.GetMaskFor(gameObject.name));
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (type != EPinballObstacle.BigBumper) return;

        var ball = collision.collider.GetComponentInParent<Pinball>();
        if (ball == null) return;

        var outwardDirection = (Vector2)ball.transform.position - (Vector2)transform.position;
        ball.SetVelocity(PinballMotionMath.CalculateBumperVelocity(
            ball.Velocity,
            outwardDirection,
            bumperMinimumExitSpeed,
            bumperSpeedBonus));
        glow?.Pulse(2.2f, 0.2f);
    }
}
