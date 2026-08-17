using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WavePanel : UIBase
{
    public override bool IsDefaultPanel => true;

    [SerializeField] private Button startButton;
    [SerializeField] private Button launchButton;
    [SerializeField] private TextMeshProUGUI launchCostText;

    public void RefreshTutorialState()
    {
        HideLegacyControls();
    }

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);
        HideLegacyControls();
    }

    private void HideLegacyControls()
    {
        if (startButton != null) startButton.gameObject.SetActive(false);
        if (launchButton != null) launchButton.gameObject.SetActive(false);
        if (launchCostText != null) launchCostText.gameObject.SetActive(false);
    }
}
