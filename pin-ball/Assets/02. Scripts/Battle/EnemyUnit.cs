using UnityEngine;

public class EnemyUnit : UnitBase
{
    public override EBattleTeam Team => EBattleTeam.Enemy;
    protected override Color IdleColor => Color.white;
    public string UnitId { get; private set; }
    public int Rank { get; private set; }
    public int BreachDamage { get; private set; }

    private readonly EnemySkillController _skills = new();
    private UnitManager _unitManager;
    private UnitAttackEffectPlayer _attackEffectPlayer;

    public void SetData(EnemyUnitData data, UnitManager unitManager = null, UnitSkillRegistry registry = null)
    {
        UnitId = data?.id ?? string.Empty;
        Rank = data?.rank ?? 0;
        BreachDamage = Mathf.Max(0, data?.BreachDamage ?? 0);
        _unitManager = unitManager;
        _attackEffectPlayer ??= GetComponent<UnitAttackEffectPlayer>();
        _skills.Initialize(data, registry ?? UnitSkillRegistry.CreateDefault());
        GetComponent<BattleUnitVisual>()?.SetUnitId(UnitId);
        if (_unitManager != null) _skills.OnBattleStart(CreateContext(null));
    }

    protected override void Tick()
    {
        _skills.Tick(CreateContext(_currentTarget), Time.time);
        if (TryKeepOrAcquireTarget()) { MoveOrAttackTarget(); return; }
        _state = EBattleUnitState.Idle;
        ClearTarget();
    }

    protected override float GetBasicAttackDamage(UnitBase target) => _skills.ModifyBasicAttackDamage(CreateContext(target), target, base.GetBasicAttackDamage(target));
    protected override void OnBasicAttackHit(UnitBase target)
    {
        _attackEffectPlayer?.Play(UnitId, target);
        _skills.OnBasicAttackHit(CreateContext(target), target);
    }
    protected override void OnDamaged() => _skills.OnDamaged(CreateContext(_currentTarget));
    protected override float ModifyIncomingDamage(float damage, UnitBase source) => _skills.ModifyIncomingDamage(CreateContext(_currentTarget), damage, source);
    protected override float ModifyCrowdControlDuration(float duration) => _skills.ModifyCrowdControlDuration(CreateContext(_currentTarget), duration);

    private UnitSkillContext CreateContext(UnitBase target)
    {
        if (_unitManager == null) return null;
        return new UnitSkillContext(this, target, _unitManager.TargetFinder, _unitManager);
    }
}
