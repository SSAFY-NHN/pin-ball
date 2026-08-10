#if UNITY_EDITOR
using NUnit.Framework;

public class BarrierDamageCalculatorTests
{
    [TestCase(8, 3, 1, 5)]
    [TestCase(2, 5, 1, 1)]
    [TestCase(0, 0, 1, 1)]
    public void Calculate_AppliesReductionAndMinimum(
        int breach,
        int reduction,
        int minimum,
        int expected)
    {
        Assert.That(
            BarrierDamageCalculator.Calculate(breach, reduction, minimum),
            Is.EqualTo(expected));
    }
}
#endif
