#if UNITY_EDITOR
using NUnit.Framework;

public sealed class BaseSkillPanelTests
{
    [TestCase(EWaveState.Pending, EBaseKnockbackSkillState.Locked, 30f, "대기")]
    [TestCase(EWaveState.Active, EBaseKnockbackSkillState.Locked, 30f, "30")]
    [TestCase(EWaveState.Active, EBaseKnockbackSkillState.Locked, 29.99f, "30")]
    [TestCase(EWaveState.Active, EBaseKnockbackSkillState.Locked, 1.01f, "2")]
    [TestCase(EWaveState.Active, EBaseKnockbackSkillState.Locked, 1f, "1")]
    [TestCase(EWaveState.Active, EBaseKnockbackSkillState.Ready, 0f, "사용 가능")]
    [TestCase(EWaveState.Active, EBaseKnockbackSkillState.Used, 0f, "사용 완료")]
    [TestCase(EWaveState.Resolving, EBaseKnockbackSkillState.Used, 0f, "사용 완료")]
    [TestCase(EWaveState.Victory, EBaseKnockbackSkillState.Ready, 0f, "대기")]
    [TestCase(EWaveState.Defeat, EBaseKnockbackSkillState.Locked, 12f, "대기")]
    public void FormatStatus_MapsWaveAndSkillState(
        EWaveState waveState,
        EBaseKnockbackSkillState skillState,
        float remainingTime,
        string expected)
    {
        Assert.That(
            BaseSkillPanel.FormatStatus(waveState, skillState, remainingTime),
            Is.EqualTo(expected));
    }

    [TestCase(EWaveState.Active, EBaseKnockbackSkillState.Ready, true, true)]
    [TestCase(EWaveState.Active, EBaseKnockbackSkillState.Ready, false, false)]
    [TestCase(EWaveState.Active, EBaseKnockbackSkillState.Locked, true, false)]
    [TestCase(EWaveState.Active, EBaseKnockbackSkillState.Used, true, false)]
    [TestCase(EWaveState.Pending, EBaseKnockbackSkillState.Ready, true, false)]
    public void ResolveInteractable_RequiresActiveReadyAndEnemy(
        EWaveState waveState,
        EBaseKnockbackSkillState skillState,
        bool hasAliveEnemy,
        bool expected)
    {
        Assert.That(
            BaseSkillPanel.ResolveInteractable(
                waveState,
                skillState,
                hasAliveEnemy),
            Is.EqualTo(expected));
    }
}
#endif
