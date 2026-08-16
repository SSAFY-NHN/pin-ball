public sealed class TutorialGameRuleController
{
    private readonly BattleManager _battleManager;
    private readonly PinballManager _pinballManager;

    private string _firstUnitId;

    public TutorialGameRuleController(
        BattleManager battleManager,
        PinballManager pinballManager)
    {
        _battleManager = battleManager;
        _pinballManager = pinballManager;
    }

    public void GrantStartingGold()
    {
        _battleManager.AddGold(_pinballManager.CurrentLaunchCost * 3);
    }

    public void ApplyGoalRule(
        TutorialStep step,
        BattleUnitSpawnData unitData)
    {
        if (unitData == null) return;

        if (step == TutorialStep.FirstLaunch)
        {
            _firstUnitId = unitData.UnitId;
        }
        else if (step == TutorialStep.SecondLaunch &&
                 !string.IsNullOrEmpty(_firstUnitId))
        {
            unitData.UnitId = _firstUnitId;
        }
    }
}
