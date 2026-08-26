#if UNITY_EDITOR
using NUnit.Framework;

public sealed class PinballGoalRewardCalculatorTests
{
    [TestCase(30, false, 3, 30)]
    [TestCase(50, true, 3, 150)]
    [TestCase(100, true, 3, 300)]
    [TestCase(-10, false, 3, 0)]
    [TestCase(30, true, 0, 30)]
    public void Calculate_ReturnsOnlyBaseTimesFocusedMultiplier(
        int baseGold, bool focused, int multiplier, int expected)
    {
        Assert.That(
            PinballGoalRewardCalculator.Calculate(baseGold, focused, multiplier),
            Is.EqualTo(expected));
    }
}
#endif
