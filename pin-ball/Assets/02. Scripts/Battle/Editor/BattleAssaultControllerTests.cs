#if UNITY_EDITOR
using System.Collections.Generic;

using NUnit.Framework;

public sealed class BattleAssaultControllerTests
{
    [Test]
    public void Advance_RequestsInitialSpawnsAtConfiguredTimes()
    {
        var controller = new BattleAssaultController();
        var spawned = new List<string>();
        controller.Start(CreateWave());

        controller.Advance(0f, 0, id =>
        {
            spawned.Add(id);
            return true;
        });
        Assert.That(spawned, Is.EqualTo(new[] { "goblin" }));

        controller.Advance(1f, 1, id =>
        {
            spawned.Add(id);
            return true;
        });
        Assert.That(spawned, Is.EqualTo(new[] { "goblin", "goblin" }));

        controller.Advance(1f, 2, id =>
        {
            spawned.Add(id);
            return true;
        });
        Assert.That(
            spawned,
            Is.EqualTo(new[] { "goblin", "goblin", "goblin_archer" }));
    }

    [Test]
    public void Advance_TransitionsAndRepeatsCurrentPhaseGroup()
    {
        var controller = new BattleAssaultController();
        var spawned = new List<string>();
        var phases = new List<EBattleAssaultPhase>();
        controller.PhaseChanged += phases.Add;
        controller.Start(CreateWaveWithoutInitialDelay());

        controller.Advance(5f, 0, id => Add(spawned, id));
        controller.Advance(5f, 0, id => Add(spawned, id));
        controller.Advance(50f, 0, id => Add(spawned, id));
        controller.Advance(30f, 0, id => Add(spawned, id));

        Assert.That(spawned, Does.Contain("goblin"));
        Assert.That(spawned, Does.Contain("wolf"));
        Assert.That(spawned, Does.Contain("troll"));
        Assert.That(phases, Is.EqualTo(new[]
        {
            EBattleAssaultPhase.Basic,
            EBattleAssaultPhase.Empowered,
            EBattleAssaultPhase.Final
        }));
    }

    [Test]
    public void Advance_AtEnemyCapSkipsWithoutAccumulatingMissedSpawns()
    {
        var controller = new BattleAssaultController();
        var spawned = new List<string>();
        controller.Start(CreateWaveWithoutInitialDelay());

        controller.Advance(10f, BattleAssaultController.MaxAliveEnemies,
            id => Add(spawned, id));
        controller.Advance(0f, 0, id => Add(spawned, id));
        Assert.That(spawned, Is.Empty);

        controller.Advance(5f, 0, id => Add(spawned, id));
        Assert.That(spawned, Is.EqualTo(new[] { "goblin", "goblin" }));
    }

    [Test]
    public void Stop_PreventsFurtherSpawnRequests()
    {
        var controller = new BattleAssaultController();
        var spawned = new List<string>();
        controller.Start(CreateWave());
        controller.Stop();

        controller.Advance(100f, 0, id => Add(spawned, id));

        Assert.That(spawned, Is.Empty);
    }

    private static BattleWaveData CreateWave()
    {
        return new BattleWaveData
        {
            InitialAssault = new List<BattleTimedSpawnData>
            {
                new()
                {
                    EnemyId = "goblin",
                    Count = 2,
                    FirstSpawnTime = 0f,
                    SpawnInterval = 1f
                },
                new()
                {
                    EnemyId = "goblin_archer",
                    Count = 1,
                    FirstSpawnTime = 2f,
                    SpawnInterval = 0f
                }
            },
            BasicReinforcement = Group(5f, "goblin", 2),
            EmpoweredReinforcement = Group(5f, "wolf", 1),
            FinalAssault = Group(5f, "troll", 1)
        };
    }

    private static BattleWaveData CreateWaveWithoutInitialDelay()
    {
        var wave = CreateWave();
        wave.InitialAssault = new List<BattleTimedSpawnData>();
        return wave;
    }

    private static BattleReinforcementGroupData Group(
        float interval,
        string enemyId,
        int count)
    {
        return new BattleReinforcementGroupData
        {
            RepeatInterval = interval,
            Enemies = new List<BattleEnemySpawnData>
            {
                new() { EnemyId = enemyId, Count = count }
            }
        };
    }

    private static bool Add(List<string> spawned, string id)
    {
        spawned.Add(id);
        return true;
    }
}
#endif
