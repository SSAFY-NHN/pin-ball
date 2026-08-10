#if UNITY_EDITOR
using System;
using System.Reflection;

using NUnit.Framework;

public class BattleFeedbackMathTests
{
    [TestCase(1.1f, "warrior", "Melee")]
    [TestCase(2.5f, "guard", "Melee")]
    [TestCase(5.5f, "archer", "Arrow")]
    [TestCase(5f, "mage", "Magic")]
    [TestCase(4.5f, "shaman", "Magic")]
    public void ResolveAttackStyle_ClassifiesUnitAttack(
        float range,
        string unitId,
        string expected)
    {
        Type type = Type.GetType("BattleFeedbackMath, Assembly-CSharp");
        Assert.That(type, Is.Not.Null);
        MethodInfo method = type.GetMethod(
            "ResolveAttackStyle",
            BindingFlags.Public | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        object result = method.Invoke(null, new object[] { range, unitId });
        Assert.That(result.ToString(), Is.EqualTo(expected));
    }
}
#endif
