using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WavePanel : UIBase
{
    public override bool IsDefaultPanel => true;

    [SerializeField] private Button startButton;
    [SerializeField] private Button launchButton;
    [SerializeField] private TextMeshProUGUI launchCostText;

    private BattleManager battleManager;
    private TMP_Text startButtonText;
    private int displayedRemainingSecond = -1;
    private Color normalLabelColor;

    public void RefreshTutorialState()
    {
        Refresh();
    }

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);
        battleManager = App.Get<BattleManager>();
        battleManager.OnStateChanged += OnBattleStateChanged;
        startButton.onClick.AddListener(OnStartButtonClicked);
        startButtonText = startButton.GetComponentInChildren<TMP_Text>(true);
        if (startButtonText != null) normalLabelColor = startButtonText.color;
        Refresh();
    }

    private void Update()
    {
        RefreshCountdownLabel();
    }

    private void OnStartButtonClicked()
    {
        battleManager.TryStartWave();
        Refresh();
    }

    private void OnBattleStateChanged(EWaveState _)
    {
        Refresh();
    }

    private void Refresh()
    {
        bool show = battleManager != null &&
                    battleManager.State == EWaveState.Pending;
        if (startButton != null)
        {
            startButton.gameObject.SetActive(show);
            startButton.interactable = show &&
                                       battleManager.CanStartCurrentWave;
        }
        if (launchButton != null) launchButton.gameObject.SetActive(false);
        if (launchCostText != null) launchCostText.gameObject.SetActive(false);
        RefreshCountdownLabel();
    }

    private void RefreshCountdownLabel()
    {
        if (battleManager == null || startButtonText == null ||
            battleManager.State != EWaveState.Pending)
        {
            return;
        }

        int remainingSecond = Mathf.CeilToInt(
            battleManager.PreparationRemainingTime);
        if (remainingSecond == displayedRemainingSecond) return;

        displayedRemainingSecond = remainingSecond;
        startButtonText.text = FormatStartButtonLabel(
            battleManager.PreparationRemainingTime);
        startButtonText.color = remainingSecond <= 5
            ? new Color32(255, 92, 92, 255)
            : remainingSecond <= 10
                ? new Color32(255, 196, 92, 255)
                : normalLabelColor;
        if (remainingSecond is > 0 and <= 5)
        {
            SoundManager.PlaySFXIfAvailable(SoundName.ButtonClick);
        }
    }

    public static string FormatStartButtonLabel(float remainingTime) =>
        $"전투 시작 ({Mathf.Max(0, Mathf.CeilToInt(remainingTime))})";

    private void OnDestroy()
    {
        if (battleManager != null)
        {
            battleManager.OnStateChanged -= OnBattleStateChanged;
        }
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClicked);
        }
    }
}
