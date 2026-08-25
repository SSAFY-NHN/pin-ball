#if UNITY_EDITOR
using System.Linq;

using NUnit.Framework;
using UnityEngine;

public sealed class BattleWaveScheduleDataTests
{
    [Test]
    public void BattleWaveJson_ContainsValidTimedScheduleForTenWaves()
    {
        TextAsset asset = Resources.Load<TextAsset>("Data/BattleWaveData");
        var collection = JsonUtility.FromJson<BattleWaveDataCollection>(asset.text);

        Assert.That(collection.waves, Has.Length.EqualTo(10));
        foreach (BattleWaveData wave in collection.waves)
        {
            Assert.That(wave.InitialAssault, Is.Not.Empty, wave.WaveName);
            Assert.That(wave.BasicReinforcement.Enemies, Is.Not.Empty, wave.WaveName);
            Assert.That(wave.EmpoweredReinforcement.Enemies, Is.Not.Empty, wave.WaveName);
            Assert.That(wave.FinalAssault.Enemies, Is.Not.Empty, wave.WaveName);
            Assert.That(wave.InitialAssault.Sum(entry => entry.Count), Is.LessThanOrEqualTo(8), wave.WaveName);
            Assert.That(wave.BasicReinforcement.Enemies.Sum(entry => entry.Count), Is.LessThanOrEqualTo(8), wave.WaveName);
            Assert.That(wave.EmpoweredReinforcement.Enemies.Sum(entry => entry.Count), Is.LessThanOrEqualTo(8), wave.WaveName);
            Assert.That(wave.FinalAssault.Enemies.Sum(entry => entry.Count), Is.LessThanOrEqualTo(8), wave.WaveName);
            Assert.That(
                wave.InitialAssault.Max(entry =>
                    entry.FirstSpawnTime +
                    entry.SpawnInterval * (entry.Count - 1)),
                Is.LessThan(60f),
                wave.WaveName);
        }

        int bossCount = collection.waves[9].InitialAssault
            .Where(entry => entry.EnemyId == "goblin_king")
            .Sum(entry => entry.Count);
        Assert.That(bossCount, Is.EqualTo(1));
        Assert.That(
            collection.waves[9].BasicReinforcement.Enemies
                .Concat(collection.waves[9].EmpoweredReinforcement.Enemies)
                .Concat(collection.waves[9].FinalAssault.Enemies)
                .Any(entry => entry.EnemyId == "goblin_king"),
            Is.False);
    }
}
#endif
