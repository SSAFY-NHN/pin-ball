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
    private EPinballState _pinballState = EPinballState.Idle;
    
    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);

        _battleManager = App.Get<BattleManager>();
        _battleManager.OnStateChanged += OnBattleStateChanged;
        _battleManager.OnActionRejected += OnActionRejected;
        _battleManager.OnGoldChanged += OnGoldChanged;
        _battleManager.OnPreparationAvailabilityChanged +=
            OnPreparationAvailabilityChanged;

        _unitManager = App.Get<UnitManager>();
        _unitManager.OnDeployedAllyCountChanged +=
            OnDeployedAllyCountChanged;
        
        _pinballManager = App.Get<PinballManager>();
        _pinballManager.OnStateChanged += OnPinballStateChanged;
        _pinballManager.OnLaunchCostChanged += OnLaunchCostChanged;

        startButton.onClick.AddListener(_battleManager.StartWave);
        launchButton.onClick.AddListener(_pinballManager.LaunchBall);

        if (feedbackText == null)
        {
            Debug.LogError("[WavePanel] feedbackText가 설정되지 않았습니다.");
        }

        if (launchCostText == null)
        {
            Debug.LogError("[WavePanel] launchCostText가 설정되지 않았습니다.");
        }
        OnBattleStateChanged(_battleManager.State);
    }
    
    private void OnBattleStateChanged(EWaveState state)
    {
        if (state != EWaveState.Pending && feedbackText != null)
        {
            feedbackText.text = string.Empty;
        }

        RefreshButtons();
    }
    
    private void OnPinballStateChanged(EPinballState state)
    {
        _pinballState = state;
        RefreshButtons();
    }

    private void OnActionRejected(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
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
        bool hasAlly =
            _unitManager != null &&
            _unitManager.DeployedAllyCount > 0;
        bool canStartWithRoster =
            _unitManager != null &&
            _unitManager.CanStartWaveWithCurrentRoster;
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
                _pinballState == EPinballState.Idle &&
                hasAlly &&
                canStartWithRoster;
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

        if (feedbackText != null && isPreparation)
        {
            const string emptyRosterMessage =
                "아군 유닛을 한 명 이상 준비해야 합니다.";
            if (!hasAlly)
            {
                feedbackText.text = emptyRosterMessage;
            }
            else if (feedbackText.text == emptyRosterMessage)
            {
                feedbackText.text = string.Empty;
            }
        }
    }

    private void OnDestroy()
    {
        if (_battleManager != null)
        {
            _battleManager.OnStateChanged -= OnBattleStateChanged;
            _battleManager.OnActionRejected -= OnActionRejected;
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
            startButton.onClick.RemoveListener(_battleManager.StartWave);
        }

        if (launchButton != null && _pinballManager != null)
        {
            launchButton.onClick.RemoveListener(_pinballManager.LaunchBall);
        }
    }
}
