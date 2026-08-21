#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class PinballRewardControllerTests
{
    private GameObject battleObject;
    private GameObject ballObject;

    [TearDown]
    public void TearDown()
    {
        if (ballObject != null) Object.DestroyImmediate(ballObject);
        if (battleObject != null) Object.DestroyImmediate(battleObject);
    }

    [Test]
    public void ApplyBumperReward_JackpotKeepsExistingGoldReward()
    {
        battleObject = new GameObject("Battle Manager");
        BattleManager battleManager = battleObject.AddComponent<BattleManager>();
        ballObject = new GameObject("Pinball");
        Pinball ball = ballObject.AddComponent<Pinball>();
        var controller = new PinballRewardController(
            battleManager,
            new PinballBallPool(null),
            new PinballItemModifiers());

        PinballRewardResult result = controller.ApplyBumperReward(
            ball,
            3,
            1f,
            3f,
            true,
            100,
            30f);

        Assert.That(result.JackpotReward, Is.EqualTo(190));
        Assert.That(result.TotalReward, Is.EqualTo(193));
        Assert.That(battleManager.Gold, Is.EqualTo(193));
    }
}
#endif
