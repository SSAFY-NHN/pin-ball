using System;

public sealed class PinballLaunchState
{
    private readonly int _baseLaunchCost;
    private readonly int _launchCostIncrease;
    private int _successfulLaunchCount;
    private int _launchCostDiscount;
    private int _minimumLaunchCost;

    public int CurrentCost
    {
        get
        {
            int escalatedCost =
                _baseLaunchCost +
                _successfulLaunchCount * _launchCostIncrease;
            int discountedCost = Math.Max(
                0,
                escalatedCost - _launchCostDiscount);
            return Math.Max(_minimumLaunchCost, discountedCost);
        }
    }

    public PinballLaunchState(int baseLaunchCost, int launchCostIncrease)
    {
        _baseLaunchCost = Math.Max(0, baseLaunchCost);
        _launchCostIncrease = Math.Max(0, launchCostIncrease);
    }

    public void RecordSuccessfulLaunch()
    {
        _successfulLaunchCount++;
    }

    public void SetCostModifiers(int discount, int minimumCost)
    {
        _launchCostDiscount = discount;
        _minimumLaunchCost = minimumCost;
    }

    public void ResetSuccessfulLaunches()
    {
        _successfulLaunchCount = 0;
    }
}
