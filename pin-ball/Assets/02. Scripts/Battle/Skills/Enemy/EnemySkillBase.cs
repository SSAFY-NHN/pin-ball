public abstract class EnemySkillBase : IUnitSkill
{
    public abstract string Id { get; }
    protected static float V(EnemySkillData data, int effect, int value) => UnitSkillValueReader.Get(data, effect, value);
    protected static float P(float value) => UnitSkillValueReader.Percent(value);
}
