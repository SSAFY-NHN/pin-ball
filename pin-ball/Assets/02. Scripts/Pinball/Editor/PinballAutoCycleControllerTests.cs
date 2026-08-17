using NUnit.Framework;
using UnityEngine;

public class PinballAutoCycleControllerTests
{
    private GameObject _firstObject;
    private GameObject _secondObject;
    private Pinball _firstBall;
    private Pinball _secondBall;

    [SetUp]
    public void SetUp()
    {
        _firstObject = new GameObject("First Ball");
        _firstBall = _firstObject.AddComponent<Pinball>();
        _secondObject = new GameObject("Second Ball");
        _secondBall = _secondObject.AddComponent<Pinball>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_firstObject);
        Object.DestroyImmediate(_secondObject);
    }

    [Test]
    public void TryTakeReady_ReturnsScheduledBallOnceAtReadyTime()
    {
        var controller = new PinballAutoCycleController();
        controller.Schedule(_firstBall, 5f);

        Assert.That(controller.TryTakeReady(4.99f, out _), Is.False);
        Assert.That(controller.TryTakeReady(5f, out var readyBall), Is.True);
        Assert.That(readyBall, Is.SameAs(_firstBall));
        Assert.That(controller.TryTakeReady(10f, out _), Is.False);
    }

    [Test]
    public void TryTakeReady_UsesEachBallsIndependentReadyTime()
    {
        var controller = new PinballAutoCycleController();
        controller.Schedule(_firstBall, 8f);
        controller.Schedule(_secondBall, 3f);

        Assert.That(controller.TryTakeReady(3f, out var firstReady), Is.True);
        Assert.That(firstReady, Is.SameAs(_secondBall));
        Assert.That(controller.TryTakeReady(7.99f, out _), Is.False);
        Assert.That(controller.TryTakeReady(8f, out var secondReady), Is.True);
        Assert.That(secondReady, Is.SameAs(_firstBall));
    }

    [Test]
    public void Schedule_DoesNotAddDuplicateReservationForSameBall()
    {
        var controller = new PinballAutoCycleController();
        controller.Schedule(_firstBall, 5f);
        controller.Schedule(_firstBall, 2f);

        Assert.That(controller.TryTakeReady(2f, out _), Is.False);
        Assert.That(controller.TryTakeReady(5f, out var readyBall), Is.True);
        Assert.That(readyBall, Is.SameAs(_firstBall));
        Assert.That(controller.TryTakeReady(10f, out _), Is.False);
    }

    [Test]
    public void Reset_RemovesAllReservations()
    {
        var controller = new PinballAutoCycleController();
        controller.Schedule(_firstBall, 1f);
        controller.Schedule(_secondBall, 2f);

        controller.Reset();

        Assert.That(controller.TryTakeReady(10f, out _), Is.False);
    }
}
