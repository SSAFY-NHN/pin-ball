using UnityEngine;
using UnityEngine.UI;

public class WavePanel : UIBase
{
    public override bool IsDefaultPanel => true;
    
    [SerializeField] private Button startButton;
    [SerializeField] private Button launchButton;
    
    private BattleManager _battleManager;
    private PinballManager _pinballManager;
    
    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);

        _battleManager = App.Get<BattleManager>();
        _battleManager.OnStateChanged += OnBattleStateChanged;
        
        _pinballManager = App.Get<PinballManager>();
        _pinballManager.OnStateChanged += OnPinballStateChanged;

        startButton.onClick.AddListener(_battleManager.StartWave);
        launchButton.onClick.AddListener(_pinballManager.LaunchBall);
    }
    
    private void OnBattleStateChanged(EWaveState state)
    {
        startButton.gameObject.SetActive(state == EWaveState.Pending);
    }
    
    private void OnPinballStateChanged(EPinballState state)
    {
        startButton.enabled = (state == EPinballState.Idle);
    }
}
