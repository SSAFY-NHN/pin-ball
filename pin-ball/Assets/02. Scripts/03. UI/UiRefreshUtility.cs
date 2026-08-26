using TMPro;
using UnityEngine;

public static class UiRefreshUtility
{
    public static bool SetTextIfChanged(TMP_Text target, string value)
    {
        if (target == null) return false;

        string nextValue = value ?? string.Empty;
        if (target.text == nextValue) return false;

        target.text = nextValue;
        return true;
    }

    public static bool SetActiveIfChanged(GameObject target, bool isActive)
    {
        if (target == null || target.activeSelf == isActive) return false;

        target.SetActive(isActive);
        return true;
    }
}
