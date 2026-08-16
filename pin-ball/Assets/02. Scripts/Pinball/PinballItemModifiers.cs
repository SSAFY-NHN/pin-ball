using UnityEngine;

public sealed class PinballItemModifiers
{
    public int GoldenBallReward { get; private set; } = 1;
    public int LaunchCostDiscount { get; private set; }
    public int MinimumLaunchCost { get; private set; }
    public float TargetMagnetDistanceMultiplier { get; private set; }
    public float TargetMagnetStrength { get; private set; }
    public int TargetMagnetCount { get; private set; }
    public int SplitCount { get; private set; }
    public float SplitSpeedMultiplier { get; private set; }
    public float FocusedPocketBonus { get; private set; }
    public float OtherPocketPenalty { get; private set; }
    public int SwapCount { get; private set; }
    public int OverloadSpawnCount { get; private set; }

    private int _goldenBumperReward;
    private int _goldenBumperMaxReward;
    private int _chargedPinRequiredHits;
    private float _chargedPinAttackBonus;
    private int _overloadRequiredHits;
    private int _overloadMaxCount;

    public void Apply(Item item)
    {
        switch (item.Key)
        {
            case EItem.GoldenBall:
                GoldenBallReward = Mathf.Max(
                    1,
                    Mathf.RoundToInt(item.Value1));
                break;
            case EItem.AutoBallFeeder:
                LaunchCostDiscount = Mathf.RoundToInt(item.Value1);
                MinimumLaunchCost = Mathf.RoundToInt(item.Value2);
                break;
            case EItem.TargetMagnet:
                TargetMagnetDistanceMultiplier = item.Value1;
                TargetMagnetStrength = item.Value2;
                TargetMagnetCount = Mathf.RoundToInt(item.Value3);
                break;
            case EItem.SplitCapsule:
                SplitCount = Mathf.RoundToInt(item.Value1);
                SplitSpeedMultiplier = item.Value2;
                break;
            case EItem.GoldenBumper:
                _goldenBumperReward = Mathf.RoundToInt(item.Value1);
                _goldenBumperMaxReward = Mathf.RoundToInt(item.Value2);
                break;
            case EItem.FocusedPocket:
                FocusedPocketBonus = item.Value1;
                OtherPocketPenalty = item.Value2;
                break;
            case EItem.SwapLever:
                SwapCount = Mathf.RoundToInt(item.Value1);
                break;
            case EItem.ChargedPin:
                _chargedPinRequiredHits = Mathf.RoundToInt(item.Value1);
                _chargedPinAttackBonus = item.Value2;
                break;
            case EItem.OverloadBumper:
                _overloadRequiredHits = Mathf.RoundToInt(item.Value1);
                OverloadSpawnCount = Mathf.RoundToInt(item.Value2);
                _overloadMaxCount = Mathf.RoundToInt(item.Value3);
                break;
        }
    }

    public int CalculateGoldenBumperReward(int accumulatedReward)
    {
        if (_goldenBumperReward <= 0 ||
            accumulatedReward >= _goldenBumperMaxReward)
        {
            return 0;
        }

        return Mathf.Min(
            _goldenBumperReward,
            _goldenBumperMaxReward - accumulatedReward);
    }

    public float CalculateChargedPinAttackBonus(int smallPinHitCount)
    {
        return smallPinHitCount >= _chargedPinRequiredHits
            ? _chargedPinAttackBonus
            : 0f;
    }

    public bool CanApplyOverload(int bigBumperHitCount, int useCount)
    {
        return _overloadRequiredHits > 0 &&
               bigBumperHitCount >= _overloadRequiredHits &&
               useCount < _overloadMaxCount;
    }
}
