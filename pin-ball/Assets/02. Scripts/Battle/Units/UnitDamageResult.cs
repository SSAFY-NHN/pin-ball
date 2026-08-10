public readonly struct UnitDamageResult
{
    public float AppliedDamage { get; }
    public float AbsorbedDamage { get; }
    public bool Died { get; }

    public UnitDamageResult(float appliedDamage, float absorbedDamage, bool died)
    {
        AppliedDamage = appliedDamage;
        AbsorbedDamage = absorbedDamage;
        Died = died;
    }
}
