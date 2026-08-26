#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class PinballResultPauseTests
{
    private GameObject ballObject;

    [TearDown]
    public void TearDown()
    {
        if (ballObject != null) Object.DestroyImmediate(ballObject);
    }

    [Test]
    public void PauseSimulation_StopsVelocityRotationAndSimulation()
    {
        ballObject = new GameObject("Ball");
        var body = ballObject.AddComponent<Rigidbody2D>();
        ballObject.AddComponent<CircleCollider2D>();
        ballObject.AddComponent<SpriteRenderer>();
        var ball = ballObject.AddComponent<Pinball>();
        ballObject.SetActive(true);
        body.simulated = true;
        body.linearVelocity = new Vector2(3f, -4f);
        body.angularVelocity = 12f;

        ball.PauseSimulation();

        Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
        Assert.That(body.angularVelocity, Is.Zero);
        Assert.That(body.simulated, Is.False);
    }
}
#endif
