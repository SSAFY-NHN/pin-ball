#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;
using UnityEngine;

public class ItemCatalogTests
{
    private static readonly IReadOnlyDictionary<string, EItem> ExpectedItems =
        new Dictionary<string, EItem>
        {
            ["golden_ball"] = EItem.GoldenBall,
            ["auto_ball_feeder"] = EItem.AutoBallFeeder,
            ["target_magnet"] = EItem.TargetMagnet,
            ["split_capsule"] = EItem.SplitCapsule,
            ["golden_bumper"] = EItem.GoldenBumper,
            ["focused_pocket"] = EItem.FocusedPocket,
            ["swap_lever"] = EItem.SwapLever,
            ["charged_pin"] = EItem.ChargedPin,
            ["overload_bumper"] = EItem.OverloadBumper,
            ["battle_clock"] = EItem.BattleClock,
            ["field_armor"] = EItem.FieldArmor,
            ["diversity_emblem"] = EItem.DiversityEmblem,
            ["barrier_reinforcement"] = EItem.BarrierReinforcement,
        };

    [Test]
    public void RuntimeCatalog_ContainsOnlyRetainedItemsWithStableKeys()
    {
        var asset = Resources.Load<TextAsset>("Data/ItemData");
        Assert.That(asset, Is.Not.Null);

        ItemData[] items = JsonUtilityHelper.FromJson<ItemData>(asset.text);
        Assert.That(items, Is.Not.Null);
        Assert.That(items.Length, Is.EqualTo(ExpectedItems.Count));
        Assert.That(items.Select(item => item.id), Is.Unique);

        var actual = items.ToDictionary(item => item.id, item => item);
        CollectionAssert.AreEquivalent(ExpectedItems.Keys, actual.Keys);

        foreach (var pair in ExpectedItems)
        {
            Assert.That(actual[pair.Key].key, Is.EqualTo((int)pair.Value), pair.Key);
            Assert.That(actual[pair.Key].type, Is.InRange(0, 2), pair.Key);
        }
    }

    [Test]
    public void RetainedEnums_PreserveKeysAndCategories()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)EItem.GoldenBall, Is.EqualTo(4));
            Assert.That((int)EItem.AutoBallFeeder, Is.EqualTo(5));
            Assert.That((int)EItem.TargetMagnet, Is.EqualTo(6));
            Assert.That((int)EItem.SplitCapsule, Is.EqualTo(7));
            Assert.That((int)EItem.GoldenBumper, Is.EqualTo(9));
            Assert.That((int)EItem.FocusedPocket, Is.EqualTo(11));
            Assert.That((int)EItem.SwapLever, Is.EqualTo(13));
            Assert.That((int)EItem.ChargedPin, Is.EqualTo(14));
            Assert.That((int)EItem.OverloadBumper, Is.EqualTo(15));
            Assert.That((int)EItem.BattleClock, Is.EqualTo(17));
            Assert.That((int)EItem.FieldArmor, Is.EqualTo(18));
            Assert.That((int)EItem.DiversityEmblem, Is.EqualTo(20));
            Assert.That((int)EItem.BarrierReinforcement, Is.EqualTo(21));
            Assert.That((int)EItemCategory.Ball, Is.Zero);
            Assert.That((int)EItemCategory.Board, Is.EqualTo(1));
            Assert.That((int)EItemCategory.Battle, Is.EqualTo(2));
        });
    }
}
#endif
