using UnityEngine;

public static class AssaultCountdownFormatter
{
    public static string Format(float elapsedTime)
    {
        float elapsed = Mathf.Max(0f, elapsedTime);
        if (elapsed >= 90f) return "최후 공세 진행 중";

        bool isEmpowered = elapsed >= 60f;
        float target = isEmpowered ? 90f : 60f;
        int remaining = Mathf.CeilToInt(target - elapsed);
        return isEmpowered
            ? $"최후 공세까지 00:{remaining:00}"
            : $"강화 증원까지 00:{remaining:00}";
    }
}
