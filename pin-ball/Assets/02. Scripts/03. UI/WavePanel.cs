using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WavePanel : UIBase
{
    public override bool IsDefaultPanel => true;
    
    [SerializeField] private Button startButton;
    [SerializeField] private Button launchButton;
    [SerializeField] private TextMeshProUGUI launchCostText;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private Color availableCostColor = Color.white;
    [SerializeField] private Color unavailableCostColor = Color.red;
    
    private BattleManager _battleManager;
    private PinballManager _pinballManager;
    private UnitManager _unitManager;
    private StatusPanel _statusPanel;
    private EPinballState _pinballState = EPinballState.Idle;
    
    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);

        _battleManager = App.Get<BattleManager>();
        _battleManager.OnStateChanged += OnBattleStateChanged;
        _battleManager.OnGoldChanged += OnGoldChanged;
        _battleManager.OnPreparationAvailabilityChanged +=
            OnPreparationAvailabilityChanged;

        _unitManager = App.Get<UnitManager>();
        _unitManager.OnDeployedAllyCountChanged +=
            OnDeployedAllyCountChanged;
        _statusPanel = manager.GetPanel<StatusPanel>();
        
        _pinballManager = App.Get<PinballManager>();
        _pinballManager.OnStateChanged += OnPinballStateChanged;
        _pinballManager.OnLaunchCostChanged += OnLaunchCostChanged;

        startButton.onClick.AddListener(OnStartButtonClicked);
        launchButton.onClick.AddListener(_pinballManager.LaunchBall);

        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }

        if (launchCostText == null)
        {
            Debug.LogError("[WavePanel] launchCostText가 설정되지 않았습니다.");
        }
        OnBattleStateChanged(_battleManager.State);
    }
    
    private void OnBattleStateChanged(EWaveState state)
    {
        RefreshButtons();
    }
    
    private void OnPinballStateChanged(EPinballState state)
    {
        _pinballState = state;
        RefreshButtons();
    }

    private void OnStartButtonClicked()
    {
        if (_unitManager != null &&
            !_unitManager.CanStartWaveWithCurrentRoster)
        {
            _statusPanel?.EmphasizeAllyCount();
        }

        _battleManager.TryStartWave();
    }

    private void OnPreparationAvailabilityChanged(bool _)
    {
        RefreshButtons();
    }

    private void OnGoldChanged(int _)
    {
        RefreshButtons();
    }

    private void OnLaunchCostChanged(int _)
    {
        RefreshButtons();
    }

    private void OnDeployedAllyCountChanged(int _)
    {
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        if (_battleManager == null) return;

        bool isPreparation = _battleManager.IsPreparationPhase;
        bool canUsePreparation =
            _battleManager.CanUsePreparationActions;
        bool canLaunchWithRoster =
            _unitManager != null &&
            _unitManager.CanLaunchPinballWithCurrentRoster;
        int launchCost = _pinballManager != null
            ? _pinballManager.CurrentLaunchCost
            : 0;
        bool canAffordLaunch =
            _battleManager.Gold >= launchCost;
        bool hasAvailableBall =
            _pinballManager != null &&
            _pinballManager.HasAvailableBall;

        if (startButton != null)
        {
            startButton.gameObject.SetActive(isPreparation);
            startButton.interactable =
                canUsePreparation &&
                _pinballState == EPinballState.Idle;
        }

        if (launchButton != null)
        {
            launchButton.interactable =
                canUsePreparation &&
                _pinballState == EPinballState.Idle &&
                hasAvailableBall &&
                canAffordLaunch &&
                canLaunchWithRoster;
        }

        if (launchCostText != null)
        {
            launchCostText.text = $"발사 {launchCost}G";
            launchCostText.color = canAffordLaunch
                ? availableCostColor
                : unavailableCostColor;
        }

    }

    private void OnDestroy()
    {
        if (_battleManager != null)
        {
            _battleManager.OnStateChanged -= OnBattleStateChanged;
            _battleManager.OnGoldChanged -= OnGoldChanged;
            _battleManager.OnPreparationAvailabilityChanged -=
                OnPreparationAvailabilityChanged;
        }

        if (_pinballManager != null)
        {
            _pinballManager.OnStateChanged -= OnPinballStateChanged;
            _pinballManager.OnLaunchCostChanged -= OnLaunchCostChanged;
        }

        if (_unitManager != null)
        {
            _unitManager.OnDeployedAllyCountChanged -=
                OnDeployedAllyCountChanged;
        }

        if (startButton != null && _battleManager != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClicked);
        }

        if (launchButton != null && _pinballManager != null)
        {
            launchButton.onClick.RemoveListener(_pinballManager.LaunchBall);
        }
    }
}
