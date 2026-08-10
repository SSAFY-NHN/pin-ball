public interface IUnitSkill
{
    string Id { get; }
}

public interface IActiveUnitSkill : IUnitSkill
{
    void Execute(UnitSkillContext context, AllySkillData data);
}

public interface IBattleStartSkill : IUnitSkill
{
    void OnBattleStart(UnitSkillContext context, EnemySkillData data);
}

public interface IUnitTickSkill : IUnitSkill
{
    void Tick(UnitSkillContext context, EnemySkillData data, float now);
}

public interface IBasicAttackDamageSkill : IUnitSkill
{
    float ModifyDamage(UnitSkillContext context, EnemySkillData data, UnitBase target, float damage);
}

public interface IBasicAttackHitSkill : IUnitSkill
{
    void OnBasicAttackHit(UnitSkillContext context, EnemySkillData data, UnitBase target, int basicAttackCount);
}

public interface IUnitDamagedSkill : IUnitSkill
{
    void OnDamaged(UnitSkillContext context, EnemySkillData data);
}

public interface IIncomingDamageSkill : IUnitSkill
{
    float ModifyIncomingDamage(UnitSkillContext context, EnemySkillData data, float damage, UnitBase source);
}

public interface ICrowdControlDurationSkill : IUnitSkill
{
    float ModifyDuration(UnitSkillContext context, EnemySkillData data, float duration);
}
