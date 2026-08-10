using System;

public static class BarrierDamageCalculator
{
    public static int Calculate(
        int totalBreachDamage,
        int damageReduction,
        int minimumDamage)
    {
        return Math.Max(
            Math.Max(1, minimumDamage),
            Math.Max(0, totalBreachDamage) - Math.Max(0, damageReduction));
    }
}
