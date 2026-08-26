using UnityEngine;

public sealed class UnitUpgradePanel : UIBase
{
    [SerializeField] private UnitUpgradeCard[] cards;

    public override bool IsDefaultPanel => true;
    public override bool IsManagedByStack => false;

    private BattleManager battleManager;
    private UnitManager unitManager;
    private TitleData titleData;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);
        battleManager = App.Get<BattleManager>();
        unitManager = App.Get<UnitManager>();
        titleData = App.Get<TitleData>();
        foreach (UnitUpgradeCard card in cards)
        {
            card?.Initialize(battleManager);
        }

        battleManager.OnInitialized += Refresh;
        battleManager.OnStateChanged += OnStateChanged;
        battleManager.OnGoldChanged += OnGoldChanged;
        battleManager.OnAllyProgressionChanged += OnProgressionChanged;
        unitManager.OnDeployedAllyCountChanged += OnRosterChanged;
        Refresh();
    }

    private void OnStateChanged(EWaveState _) => Refresh();
    private void OnGoldChanged(int _) => Refresh();
    private void OnProgressionChanged(string _) => Refresh();
    private void OnRosterChanged(int _) => Refresh();

    private void Refresh()
    {
        if (battleManager == null) return;
        bool visible = battleManager.IsInitialized &&
                       battleManager.IsPreparationPhase &&
                       !battleManager.IsRunEnded;
        UiRefreshUtility.SetActiveIfChanged(gameObject, visible);
        if (!visible) return;
        foreach (UnitUpgradeCard card in cards)
        {
            card?.Refresh(battleManager, unitManager, titleData);
        }
    }

    private void OnDestroy()
    {
        if (battleManager != null)
        {
            battleManager.OnInitialized -= Refresh;
            battleManager.OnStateChanged -= OnStateChanged;
            battleManager.OnGoldChanged -= OnGoldChanged;
            battleManager.OnAllyProgressionChanged -= OnProgressionChanged;
        }
        if (unitManager != null)
        {
            unitManager.OnDeployedAllyCountChanged -= OnRosterChanged;
        }
    }
}
