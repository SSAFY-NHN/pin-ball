using UnityEngine;

public sealed class PinballRewardController
{
    private readonly BattleManager _battleManager;
    private readonly PinballBallPool _ballPool;
    private readonly PinballItemModifiers _itemModifiers;

    public PinballRewardController(
        BattleManager battleManager,
        PinballBallPool ballPool,
        PinballItemModifiers itemModifiers)
    {
        _battleManager = battleManager;
        _ballPool = ballPool;
        _itemModifiers = itemModifiers;
    }

    public int ApplyBumperReward(Pinball ball, int bumperIncome)
    {
        if (ball == null) return 0;

        int baseReward = Mathf.Max(0, bumperIncome) *
                         _itemModifiers.GoldenBallReward;
        _battleManager.AddGold(baseReward);

        int bumperReward = _itemModifiers.CalculateGoldenBumperReward(
            ball.GoldenBumperGold);
        if (bumperReward > 0)
        {
            ball.GoldenBumperGold += bumperReward;
            _battleManager.AddGold(bumperReward);
        }

        ApplySplitCapsule(ball);
        return baseReward + bumperReward;
    }

    private void ApplySplitCapsule(Pinball source)
    {
        if (_itemModifiers.SplitCount <= 0 ||
            source.IsClone ||
            source.HasSplit)
        {
            return;
        }

        source.HasSplit = true;
        for (var i = 0; i < _itemModifiers.SplitCount; i++)
        {
            if (!_ballPool.TryAcquireClone(out var clone)) break;

            clone.Activate(
                source.transform.position,
                source.Velocity.normalized,
                true);
            clone.SetVelocity(
                source.Velocity *
                _itemModifiers.SplitSpeedMultiplier);
        }
    }
}
