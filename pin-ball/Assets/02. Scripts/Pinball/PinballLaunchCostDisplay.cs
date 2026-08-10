using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PinballLaunchCostDisplay : MonoBehaviour
{
    private static readonly Color AvailableColor = new(1f, 0.82f, 0.24f, 1f);
    private static readonly Color UnavailableColor = new(1f, 0.32f, 0.38f, 1f);

    private PinballManager _pinballManager;
    private BattleManager _battleManager;
    private TextMeshPro _costText;

    private void Start()
    {
        _pinballManager = App.Get<PinballManager>();
        _battleManager = App.Get<BattleManager>();
        CreateCostText();

        _pinballManager.OnLaunchCostChanged += Refresh;
        _battleManager.OnGoldChanged += OnGoldChanged;
        Refresh(_pinballManager.CurrentLaunchCost);
    }

    private void CreateCostText()
    {
        Transform leverGlow = transform.Find("PlungerLever/PlungerLeverGlow");
        Transform labelParent = leverGlow != null ? leverGlow : transform;
        Transform existingLabel = labelParent.Find("LaunchCost");
        var labelObject = existingLabel != null
            ? existingLabel.gameObject
            : new GameObject("LaunchCost", typeof(TextMeshPro));
        labelObject.transform.SetParent(labelParent, false);
        labelObject.transform.localPosition = new Vector3(0.111f, -0.215f, -0.2f);
        labelObject.transform.localRotation = Quaternion.Euler(0f, -180f, -49.342f);
        labelObject.transform.localScale = Vector3.one * 0.18f;

        _costText = labelObject.GetComponent<TextMeshPro>();
        if (_costText == null) _costText = labelObject.AddComponent<TextMeshPro>();
        _costText.rectTransform.sizeDelta = new Vector2(20f, 5f);
        _costText.alignment = TextAlignmentOptions.Center;
        _costText.fontSize = 12f;
        _costText.fontStyle = FontStyles.Bold;
        _costText.textWrappingMode = TextWrappingModes.NoWrap;
        _costText.outlineWidth = 0.22f;
        _costText.outlineColor = new Color32(45, 14, 65, 255);
        _costText.sortingOrder = 30;
    }

    private void OnGoldChanged(int _)
    {
        Refresh(_pinballManager.CurrentLaunchCost);
    }

    private void Refresh(int launchCost)
    {
        if (_costText == null || _battleManager == null) return;

        _costText.text = $"{launchCost}G";
        _costText.color = _battleManager.Gold >= launchCost
            ? AvailableColor
            : UnavailableColor;
    }

    private void OnDestroy()
    {
        if (_pinballManager != null)
        {
            _pinballManager.OnLaunchCostChanged -= Refresh;
        }

        if (_battleManager != null)
        {
            _battleManager.OnGoldChanged -= OnGoldChanged;
        }
    }
}
