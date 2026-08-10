#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class ArcaneGlowMathTests
{
    [Test]
    public void CalculateMaskScale_MatchesSourceWorldBounds()
    {
        Assert.That(
            ArcaneGlowMath.CalculateMaskScale(new Vector2(2f, 1f), new Vector2(1f, 2f)),
            Is.EqualTo(new Vector2(2f, 0.5f)));
    }

    [Test]
    public void EvaluatePulse_ReturnsBaseAfterDuration()
    {
        Assert.That(ArcaneGlowMath.EvaluatePulse(0.8f, 2f, 0.2f, 0.3f), Is.EqualTo(0.8f));
    }

    [Test]
    public void CalculateLauncherIntensity_OrdersInteractionStates()
    {
        float unloaded = ArcaneGlowMath.CalculateLauncherIntensity(
            false, false, 0f, 0.5f, 0.2f, 1.15f, 1.55f, 2.1f, 0.12f);
        float loadedIdle = ArcaneGlowMath.CalculateLauncherIntensity(
            true, false, 0f, 0.5f, 0.2f, 1.15f, 1.55f, 2.1f, 0.12f);
        float hover = ArcaneGlowMath.CalculateLauncherIntensity(
            true, true, 0f, 0.5f, 0.2f, 1.15f, 1.55f, 2.1f, 0.12f);
        float fullPull = ArcaneGlowMath.CalculateLauncherIntensity(
            true, true, 1f, 0.5f, 0.2f, 1.15f, 1.55f, 2.1f, 0.12f);

        Assert.That(unloaded, Is.LessThan(loadedIdle));
        Assert.That(loadedIdle, Is.LessThan(hover));
        Assert.That(hover, Is.LessThan(fullPull));
    }
}
#endif
