#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;
using UnityEngine;

public class UnitPlacementServiceTests
{
    private readonly List<GameObject> _objects = new();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject gameObject in _objects)
        {
            if (gameObject != null) Object.DestroyImmediate(gameObject);
        }

        _objects.Clear();
    }

    [TestCase(5f, 4f, true)]
    [TestCase(5.4f, 4f, true)]
    [TestCase(6f, 4f, false)]
    public void IsGridPositionOccupied_UsesMinimumDistance(
        float x,
        float y,
        bool expected)
    {
        MethodInfo method = typeof(UnitPlacementService).GetMethod(
            "IsGridPositionOccupied",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        var result = (bool)method.Invoke(
            null,
            new object[]
            {
                new Vector3(x, y, 0f),
                new List<Vector3> { new(5f, 4f, 0f) },
                0.5f
            });

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void TryGetSavedPosition_ReturnsExactSavedVector()
    {
        UnitPlacementService service = CreateService();
        AllyUnit ally = CreateAlly("ally");
        var saved = new Vector3(7.25f, 3.5f, -2f);

        bool didSave = service.TrySave(ally, saved);
        bool found = service.TryGetSavedPosition(ally, out Vector3 result);

        Assert.That(didSave, Is.True);
        Assert.That(found, Is.True);
        Assert.That(result, Is.EqualTo(saved));
    }

    [Test]
    public void Remove_FreesSavedPosition()
    {
        UnitPlacementService service = CreateService();
        AllyUnit ally = CreateAlly("ally");
        Assert.That(service.TrySave(ally, new Vector3(6f, 4f, 0f)), Is.True);

        service.Remove(ally);

        Assert.That(service.TryGetSavedPosition(ally, out _), Is.False);
    }

    [Test]
    public void TryFindFreeGridSlot_SkipsOccupiedPosition()
    {
        UnitPlacementService service = CreateService();
        AllyUnit existingAlly = CreateAlly("existing-ally");
        const float padding = 0.5f;

        Assert.That(
            service.TryFindFreeGridSlot(padding, out Vector3 occupied),
            Is.True);
        Assert.That(service.TrySave(existingAlly, occupied), Is.True);

        Assert.That(
            service.TryFindFreeGridSlot(padding, out Vector3 free),
            Is.True);
        Assert.That(free, Is.Not.EqualTo(occupied));
        Assert.That(
            Vector2.Distance(free, occupied),
            Is.GreaterThanOrEqualTo(padding * 2f + 0.15f));
    }

    private UnitPlacementService CreateService()
    {
        var gameObject = new GameObject("battle-area");
        gameObject.SetActive(false);
        _objects.Add(gameObject);
        var bounds = gameObject.AddComponent<BattleAreaBounds>();
        SetField(bounds, "_worldMin", new Vector2(0f, 0f));
        SetField(bounds, "_worldMax", new Vector2(10f, 8f));
        SetField(bounds, "<IsValid>k__BackingField", true);
        return new UnitPlacementService(bounds);
    }

    private AllyUnit CreateAlly(string name)
    {
        var gameObject = new GameObject(name);
        _objects.Add(gameObject);
        return gameObject.AddComponent<AllyUnit>();
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }
}
#endif
