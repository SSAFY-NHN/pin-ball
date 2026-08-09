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
}
#endif
