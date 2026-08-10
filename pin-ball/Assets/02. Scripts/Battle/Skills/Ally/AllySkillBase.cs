public abstract class AllySkillBase : IActiveUnitSkill
{
    public abstract string Id { get; }
    public abstract void Execute(UnitSkillContext context, AllySkillData data);
    protected static float V(AllySkillData data, int effect, int value) => UnitSkillValueReader.Get(data, effect, value);
    protected static float P(float value) => UnitSkillValueReader.Percent(value);
}
