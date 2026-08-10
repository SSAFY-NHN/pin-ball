using System;
using System.Collections.Generic;

public sealed class UnitSkillRegistry
{
    private readonly Dictionary<string, Func<IUnitSkill>> _factories;

    public UnitSkillRegistry(IEnumerable<Func<IUnitSkill>> factories)
    {
        _factories = new Dictionary<string, Func<IUnitSkill>>(StringComparer.Ordinal);
        foreach (var factory in factories)
        {
            var skill = factory();
            if (!_factories.TryAdd(skill.Id, factory)) throw new ArgumentException($"Duplicate skill id: {skill.Id}");
        }
    }

    public bool TryCreate(string id, out IUnitSkill skill)
    {
        if (!string.IsNullOrEmpty(id) && _factories.TryGetValue(id, out var factory))
        {
            skill = factory();
            return true;
        }
        skill = null;
        return false;
    }

    public static UnitSkillRegistry CreateDefault() => new(new Func<IUnitSkill>[]
    {
        () => new ShieldJudgmentSkill(), () => new BloodWhirlwindSkill(), () => new ArrowRainSkill(),
        () => new PiercingShotSkill(), () => new ExplosiveFireballSkill(), () => new FrostStormSkill(),
        () => new PiercingChargeSkill(), () => new PhalanxFormationSkill(), () => new WolfSprintSkill(),
        () => new FocusedFireSkill(), () => new ShieldBlockSkill(), () => new OrcRageSkill(),
        () => new DarkBlastSkill(), () => new ShadowLeapSkill(), () => new TrollRegenerationSkill(),
        () => new GroundSlamSkill(), () => new WeakeningCurseSkill(), () => new SummonMinionsSkill(),
        () => new KingSlamSkill(), () => new FinalOrderSkill()
    });
}
