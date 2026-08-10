#if UNITY_EDITOR
using NUnit.Framework;

public class ArcaneSpriteEffectTests
{
    [Test]
    public void NormalizedLifetime_ReachesOneAtDuration()
    {
        Assert.That(ArcaneSpriteEffect.NormalizedLifetime(0.25f, 0.25f), Is.EqualTo(1f));
    }
}
#endif
