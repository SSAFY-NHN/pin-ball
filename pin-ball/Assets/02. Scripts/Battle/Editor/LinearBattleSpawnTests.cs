#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class LinearBattleSpawnTests
{
    [Test]
    public void ResolveSpawnPosition_UsesExactTeamSpawnAndSharedBattleLine()
    {
        Vector3 ally = UnitSpawner.ResolveSpawnPosition(
            EBattleTeam.Ally,
            new Vector3(-6f, 2f, 0f),
            null,
            0f);
        Vector3 enemy = UnitSpawner.ResolveSpawnPosition(
            EBattleTeam.Enemy,
            new Vector3(6f, -4f, 0f),
            new Vector3(1f, 9f, 0f),
            0f);

        Assert.That(ally, Is.EqualTo(new Vector3(-6f, 0f, 0f)));
        Assert.That(enemy, Is.EqualTo(new Vector3(6f, 0f, 0f)));
    }
}
#endif
