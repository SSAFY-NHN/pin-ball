using UnityEngine;

public static class PinballGoalRewardCalculator
{
    public static int Calculate(int baseGoldReward, bool isFocused, int focusedMultiplier)
    {
        int safeBase = Mathf.Max(0, baseGoldReward);
        int multiplier = isFocused ? Mathf.Max(1, focusedMultiplier) : 1;
        long reward = (long)safeBase * multiplier;
        return reward > int.MaxValue ? int.MaxValue : (int)reward;
    }
}
