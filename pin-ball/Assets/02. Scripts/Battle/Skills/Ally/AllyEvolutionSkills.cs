using UnityEngine;

public sealed class IronFistSkill : AllySkillBase
{
    public override string Id => "iron_fist";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        if (c.PrimaryTarget == null) return;
        c.PrimaryTarget.TakeDamage(c.Caster.AttackDamage * P(V(d, 0, 1)), 0f, c.Caster);
        c.Caster.ApplyShield(c.Caster.MaxHp * P(V(d, 1, 2)), V(d, 1, 1));
    }
}

public sealed class FeralRampageSkill : AllySkillBase
{
    public override string Id => "feral_rampage";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        c.TargetFinder.GetAliveEnemiesInRadius(c.Caster.transform.position, V(d, 0, 1), c.Targets);
        foreach (var enemy in c.Targets) enemy.TakeDamage(c.Caster.AttackDamage * P(V(d, 0, 2)), 0f, c.Caster);
        c.Caster.Heal(c.Caster.MaxHp * P(V(d, 1, 1)));
        c.Caster.ApplyAttackRateMultiplier(1f + P(V(d, 2, 2)), V(d, 2, 1));
    }
}

public sealed class ShadowSlashSkill : AllySkillBase
{
    public override string Id => "shadow_slash";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        var target = c.TargetFinder.FindFarthestAliveEnemy(c.Caster.transform.position) ?? c.PrimaryTarget;
        if (target == null) return;
        c.Caster.transform.position = target.transform.position - Vector3.right * 0.5f;
        target.TakeDamage(c.Caster.AttackDamage * P(V(d, 0, 1)), P(V(d, 0, 2)), c.Caster);
    }
}

public sealed class CloneFlurrySkill : AllySkillBase
{
    public override string Id => "clone_flurry";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        var target = c.TargetFinder.FindFarthestAliveEnemy(c.Caster.transform.position) ?? c.PrimaryTarget;
        if (target == null) return;
        c.Caster.transform.position = target.transform.position - Vector3.right * 0.5f;
        c.TargetFinder.GetAliveEnemiesInRadius(target.transform.position, V(d, 0, 1), c.Targets);
        foreach (var enemy in c.Targets) enemy.TakeDamage(c.Caster.AttackDamage * P(V(d, 0, 2)), P(V(d, 0, 3)), c.Caster);
        c.Caster.ApplyDamageReduction(P(V(d, 1, 2)), V(d, 1, 1));
    }
}

public sealed class BattleCrySkill : AllySkillBase
{
    public override string Id => "battle_cry";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        c.TargetFinder.GetAliveEnemiesInRadius(c.Caster.transform.position, V(d, 0, 1), c.Targets);
        foreach (var enemy in c.Targets) enemy.TakeDamage(c.Caster.AttackDamage * P(V(d, 0, 2)), 0f, c.Caster);
        c.Caster.ApplyDefenseMultiplier(1f + P(V(d, 1, 2)), V(d, 1, 1));
    }
}

public sealed class IronFormationSkill : AllySkillBase
{
    public override string Id => "iron_formation";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        if (c.PrimaryTarget == null) return;
        var direction = (c.PrimaryTarget.transform.position - c.Caster.transform.position).normalized;
        c.TargetFinder.GetEnemiesInLine(c.Caster.transform.position, direction, V(d, 0, 1), V(d, 0, 2), c.Targets);
        foreach (var enemy in c.Targets) { enemy.TakeDamage(c.Caster.AttackDamage * P(V(d, 1, 1)), 0f, c.Caster); enemy.ApplyStun(V(d, 1, 2)); }
        c.TargetFinder.GetAliveAlliesInRadius(c.Caster.transform.position, V(d, 2, 1), c.Targets);
        foreach (var ally in c.Targets) { ally.ApplyShield(ally.MaxHp * P(V(d, 2, 2)), V(d, 2, 3)); ally.ApplyDamageReduction(P(V(d, 3, 2)), V(d, 3, 1)); }
    }
}

public sealed class TranscendentChargeSkill : AllySkillBase
{
    public override string Id => "transcendent_charge";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        if (c.PrimaryTarget == null) return;
        var direction = (c.PrimaryTarget.transform.position - c.Caster.transform.position).normalized;
        c.TargetFinder.GetEnemiesInLine(c.Caster.transform.position, direction, V(d, 0, 1), V(d, 0, 2), c.Targets);
        foreach (var enemy in c.Targets) { enemy.TakeDamage(c.Caster.AttackDamage * P(V(d, 1, 1)), P(V(d, 1, 2)), c.Caster); enemy.ApplyStun(V(d, 2, 1)); }
        c.Caster.transform.position += direction * V(d, 0, 1);
    }
}

public sealed class RapidFireSkill : AllySkillBase
{
    public override string Id => "rapid_fire";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        if (c.PrimaryTarget == null) return;
        int shots = Mathf.Max(1, Mathf.RoundToInt(V(d, 0, 1)));
        for (int i = 0; i < shots && c.PrimaryTarget.IsAlive; i++) c.PrimaryTarget.TakeDamage(c.Caster.AttackDamage * P(V(d, 0, 2)), 0f, c.Caster);
        c.Caster.ApplyAttackRateMultiplier(1f + P(V(d, 1, 2)), V(d, 1, 1));
    }
}

public sealed class ExplosiveBarrageSkill : AllySkillBase
{
    public override string Id => "explosive_barrage";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        if (c.PrimaryTarget == null) return;
        c.TargetFinder.GetAliveEnemiesInRadius(c.PrimaryTarget.transform.position, V(d, 0, 1), c.Targets);
        foreach (var enemy in c.Targets) enemy.TakeDamage(c.Caster.AttackDamage * P(V(d, 0, 2)), P(V(d, 0, 3)), c.Caster);
    }
}

public sealed class FinishingPierceSkill : AllySkillBase
{
    public override string Id => "finishing_pierce";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        if (c.PrimaryTarget == null) return;
        var direction = c.PrimaryTarget.transform.position - c.Caster.transform.position;
        c.TargetFinder.GetEnemiesInLine(c.Caster.transform.position, direction, V(d, 0, 1), V(d, 0, 2), c.Targets);
        for (int i = 0; i < c.Targets.Count; i++) c.Targets[i].TakeDamage(c.Caster.AttackDamage * P(V(d, 1, 1) + i * V(d, 1, 2)), P(V(d, 2, 1)), c.Caster);
    }
}

public sealed class HealingLightSkill : AllySkillBase
{
    public override string Id => "healing_light";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        var ally = c.TargetFinder.FindLowestHpAliveAlly() ?? c.Caster;
        ally.Heal(ally.MaxHp * P(V(d, 0, 1)));
    }
}

public sealed class PrayerOfLifeSkill : AllySkillBase
{
    public override string Id => "prayer_of_life";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        c.TargetFinder.GetAliveAlliesInRadius(c.Caster.transform.position, V(d, 0, 1), c.Targets);
        foreach (var ally in c.Targets) { ally.Heal(ally.MaxHp * P(V(d, 0, 2))); ally.ApplyShield(ally.MaxHp * P(V(d, 1, 2)), V(d, 1, 1)); }
    }
}

public sealed class ManaStormSkill : AllySkillBase
{
    public override string Id => "mana_storm";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        if (c.PrimaryTarget == null) return;
        c.TargetFinder.GetAliveEnemiesInRadius(c.PrimaryTarget.transform.position, V(d, 0, 1), c.Targets);
        foreach (var enemy in c.Targets) { enemy.TakeDamage(c.Caster.AttackDamage * P(V(d, 0, 2)), P(V(d, 0, 3)), c.Caster); enemy.ApplyStun(V(d, 1, 1)); enemy.ApplySlowAfterDelay(1f - P(V(d, 2, 2)), 1f - P(V(d, 2, 3)), V(d, 2, 1), V(d, 1, 1)); }
    }
}
