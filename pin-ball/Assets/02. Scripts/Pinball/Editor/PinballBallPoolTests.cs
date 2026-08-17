using NUnit.Framework;
using UnityEngine;

public sealed class PinballBallPoolTests
{
    private GameObject _permanentObject;
    private GameObject _cloneObject;
    private Pinball _permanentBall;
    private Pinball _cloneBall;

    [SetUp]
    public void SetUp()
    {
        _permanentObject = new GameObject("Permanent Ball");
        _permanentBall = _permanentObject.AddComponent<Pinball>();
        _cloneObject = new GameObject("Clone Ball");
        _cloneBall = _cloneObject.AddComponent<Pinball>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_permanentObject);
        Object.DestroyImmediate(_cloneObject);
    }

    [Test]
    public void CloneAcquire_NeverConsumesPermanentSlot()
    {
        var pool = new PinballBallPool(
            new[] { _permanentBall },
            new[] { _cloneBall });

        Assert.That(pool.TryAcquireClone(out var clone), Is.True);
        Assert.That(clone, Is.SameAs(_cloneBall));
        Assert.That(pool.TryAcquirePermanent(out var permanent), Is.True);
        Assert.That(permanent, Is.SameAs(_permanentBall));
    }

    [Test]
    public void ReleasedClone_ReturnsToClonePoolWithoutPermanentReservation()
    {
        var pool = new PinballBallPool(
            new[] { _permanentBall },
            new[] { _cloneBall });
        pool.TryAcquireClone(out var clone);

        Assert.That(pool.Release(clone), Is.EqualTo(EPinballReleaseType.Clone));
        Assert.That(pool.TryAcquireClone(out var reacquired), Is.True);
        Assert.That(reacquired, Is.SameAs(_cloneBall));
    }

    [Test]
    public void ReleasedPermanent_WaitsForExplicitReactivation()
    {
        var pool = new PinballBallPool(
            new[] { _permanentBall },
            new[] { _cloneBall });
        pool.TryAcquirePermanent(out var permanent);

        Assert.That(pool.Release(permanent), Is.EqualTo(EPinballReleaseType.Permanent));
        Assert.That(pool.TryAcquirePermanent(out _), Is.False);
        Assert.That(pool.TryReactivatePermanent(permanent), Is.True);
    }
}
