using UnityEngine;
using DG.Tweening;

public enum EPinballObstacle
{
    SmallPin,
    BigBumper
}

public class PinballObstacle : MonoBehaviour
{
    [SerializeField] private EPinballObstacle type;
    [SerializeField] private bool isJackpotBumper;
    [SerializeField, Min(0f)] private float bumperMinimumExitSpeed = 6f;
    [SerializeField, Min(0f)] private float bumperSpeedBonus = 1f;

    public EPinballObstacle Type => type;
    public bool IsJackpotBumper =>
        type == EPinballObstacle.BigBumper && isJackpotBumper;
    private ArcaneMaskGlowController glow;

    private void Awake()
    {
        if (type != EPinballObstacle.BigBumper) return;
        glow = GetComponent<ArcaneMaskGlowController>();
    }

    internal void PlayJackpotFeedback()
    {
        if (!IsJackpotBumper) return;

        glow?.Pulse(4f, 0.42f);
        transform.DOKill(true);
        transform.DOPunchScale(
            new Vector3(0.3f, 0.3f, 0f),
            0.42f,
            8,
            0.45f);
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
        transform.DOKill(true);
        transform.DOPunchScale(
            new Vector3(0.18f, -0.12f, 0f),
            0.25f,
            6,
            0.5f);
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}
