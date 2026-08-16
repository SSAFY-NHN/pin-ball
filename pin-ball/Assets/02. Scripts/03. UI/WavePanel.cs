using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WavePanel : UIBase
{
    public override bool IsDefaultPanel => true;
    
    [SerializeField] private Button startButton;
    [SerializeField] private Button launchButton;
    [SerializeField] private TextMeshProUGUI launchCostText;
    [SerializeField] private Color availableCostColor = Color.white;
    [SerializeField] private Color unavailableCostColor = Color.red;
    
    private BattleManager _battleManager;
    private PinballManager _pinballManager;
    private UnitManager _unitManager;
    private StatusPanel _statusPanel;
    private EPinballState _pinballState = EPinballState.Idle;
    private readonly WaveButtonStateController _buttonStateController = new();
    private bool _hasValidReferences;

    public Button StartButton => startButton;

    public void RefreshTutorialState()
    {
        RefreshButtons();
    }
    
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

        _hasValidReferences = ValidateReferences();
        if (_hasValidReferences)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
            launchButton.onClick.AddListener(_pinballManager.LaunchBall);
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

    public static bool IsLaunchAvailable(
        bool canUsePreparation,
        EPinballState pinballState,
        bool hasAvailableBall,
        bool canAffordLaunch)
    {
        return WaveButtonStateController.IsLaunchAvailable(
            canUsePreparation,
            pinballState,
            hasAvailableBall,
            canAffordLaunch);
    }

    private void RefreshButtons()
    {
        if (_battleManager == null || !_hasValidReferences) return;

        int launchCost = _pinballManager != null
            ? _pinballManager.CurrentLaunchCost
            : 0;
        WaveButtonState state = _buttonStateController.Calculate(
            _battleManager.IsPreparationPhase,
            _battleManager.CanUsePreparationActions,
            _pinballState,
            _pinballManager != null && _pinballManager.HasAvailableBall,
            _battleManager.Gold,
            launchCost);

        startButton.gameObject.SetActive(state.ShowStartButton);
        startButton.interactable = state.EnableStartButton;
        launchButton.interactable = state.EnableLaunchButton;
        launchCostText.text = $"발사 {state.LaunchCost}G";
        launchCostText.color = state.CanAffordLaunch
            ? availableCostColor
            : unavailableCostColor;
    }

    private bool ValidateReferences()
    {
        bool valid =
            startButton != null &&
            launchButton != null &&
            launchCostText != null;
        if (!valid)
        {
            Debug.LogError(
                "[WavePanel] startButton, launchButton, and " +
                "launchCostText must be assigned.");
        }

        return valid;
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
