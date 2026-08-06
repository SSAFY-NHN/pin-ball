using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleStatusPanel : UIBase
{
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI playerHpText;
    [SerializeField] private TextMeshProUGUI goldText;
    
    [SerializeField] private Button startButton;
    [SerializeField] private Button launchButton;

    private BattleManager _battleManager;
    private PinballManager _pinballManager;
    private int _maxHp;
    private int _totalWaveCount;

    public override bool IsDefaultPanel => true;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);

        _battleManager = App.Get<BattleManager>();
        _battleManager.OnStateChanged += OnBattleStateChanged;
        _battleManager.OnWaveChanged += OnWaveChanged;
        _battleManager.OnHpChanged += OnHpChanged;
        _battleManager.OnGoldChanged += OnGoldChanged;
        
        _pinballManager = App.Get<PinballManager>();
        _pinballManager.OnStateChanged += OnPinballStateChanged;

        _maxHp = _battleManager.playerMaxHp;
        _totalWaveCount = 1;

        startButton.onClick.AddListener(_battleManager.StartWave);        
        launchButton.onClick.AddListener(() => _pinballManager.LaunchBall(new Vector2(6.4f, 10f)));
    }

    private void OnBattleStateChanged(EWaveState state)
    {
        startButton.gameObject.SetActive(state == EWaveState.Pending);
    }

    private void OnWaveChanged(int waveIndex)
    {
        var current = Mathf.Clamp(waveIndex + 1, 0, Mathf.Max(0, _totalWaveCount));
        waveText.text = $"Wave: {current}/{_totalWaveCount}";
    }

    private void OnHpChanged(int hp)
    {
        playerHpText.text = $"Player HP: {Mathf.Max(0, hp)}/{Mathf.Max(1, _maxHp)}";
    }

    private void OnGoldChanged(int gold)
    {
        goldText.text = $"Gold: {Mathf.Max(0, gold)}";
    }
    
    private void OnPinballStateChanged(EPinballState state)
    {
        startButton.enabled = (state == EPinballState.Idle);
    }
}
