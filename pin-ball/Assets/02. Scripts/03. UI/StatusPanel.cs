using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusPanel : UIBase
{
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI playerHpText;
    [SerializeField] private TextMeshProUGUI goldText;

    private BattleManager _battleManager;
    private int _maxHp;
    private int _totalWaveCount;

    public override bool IsDefaultPanel => true;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);

        _battleManager = App.Get<BattleManager>();
        _battleManager.OnWaveChanged += OnWaveChanged;
        _battleManager.OnHpChanged += OnHpChanged;
        _battleManager.OnGoldChanged += OnGoldChanged;

        _maxHp = _battleManager.playerMaxHp;
        _totalWaveCount = 1;
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
}
