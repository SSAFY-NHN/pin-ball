public static class UnitSkillValueReader
{
    public static float Get(AllySkillData data, int effectIndex, int valueIndex)
    {
        if (data?.effects == null || effectIndex < 0 || effectIndex >= data.effects.Length) return 0f;
        var effect = data.effects[effectIndex];
        return valueIndex switch { 1 => effect.value1, 2 => effect.value2, 3 => effect.value3, _ => 0f };
    }

    public static float Get(EnemySkillData data, int effectIndex, int valueIndex)
    {
        if (data?.effects == null || effectIndex < 0 || effectIndex >= data.effects.Length) return 0f;
        var effect = data.effects[effectIndex];
        return valueIndex switch { 1 => effect.value1, 2 => effect.value2, 3 => effect.value3, _ => 0f };
    }

    public static float Percent(float value) => value * 0.01f;
}
