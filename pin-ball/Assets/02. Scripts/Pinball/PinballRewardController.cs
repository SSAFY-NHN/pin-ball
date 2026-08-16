using UnityEngine;

public sealed class PinballRewardController
{
    private readonly BattleManager _battleManager;
    private readonly UnitManager _unitManager;
    private readonly PinballBallPool _ballPool;
    private readonly PinballItemModifiers _itemModifiers;

    public PinballRewardController(
        BattleManager battleManager,
        UnitManager unitManager,
        PinballBallPool ballPool,
        PinballItemModifiers itemModifiers)
    {
        _battleManager = battleManager;
        _unitManager = unitManager;
        _ballPool = ballPool;
        _itemModifiers = itemModifiers;
    }

    public int ApplyBumperReward(Pinball ball)
    {
        if (ball == null) return 0;

        int baseReward = _itemModifiers.GoldenBallReward;
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

    public void ApplyGoalReward(
        Pinball ball,
        BattleUnitSpawnData unitData)
    {
        if (ball == null || unitData == null) return;

        float attackBonus =
            _itemModifiers.CalculateChargedPinAttackBonus(
                ball.SmallPinHitCount);
        _unitManager.SpawnAlly(unitData, attackBonus);

        if (!_itemModifiers.CanApplyOverload(
                ball.BigBumperHitCount,
                ball.OverloadUseCount)) return;

        for (var i = 0; i < _itemModifiers.OverloadSpawnCount; i++)
        {
            _unitManager.SpawnAlly(unitData, attackBonus);
        }

        ball.OverloadUseCount++;
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
            if (!_ballPool.TryAcquireActive(out var clone)) break;

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
