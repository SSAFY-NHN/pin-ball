using UnityEngine;
using UnityEngine.UI;

public class WavePanel : UIBase
{
    public override bool IsDefaultPanel => true;

    [SerializeField] private Button startButton;

    private BattleManager battleManager;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);
        battleManager = App.Get<BattleManager>();
        battleManager.OnStateChanged += OnBattleStateChanged;
        startButton.onClick.AddListener(OnStartButtonClicked);
        Refresh();
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
    }

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
