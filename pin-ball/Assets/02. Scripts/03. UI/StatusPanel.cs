using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum EWaveHudNodeState
{
    Idle,
    Current,
    Complete,
    Elite05,
    Elite09,
    Boss10
}

public sealed class WaveHudState
{
    public EWaveHudNodeState ResolveNodeState(
        int currentWave,
        int nodeWave)
    {
        if (nodeWave < currentWave)
        {
            return EWaveHudNodeState.Complete;
        }

        if (nodeWave > currentWave)
        {
            return EWaveHudNodeState.Idle;
        }

        return nodeWave switch
        {
            5 => EWaveHudNodeState.Elite05,
            9 => EWaveHudNodeState.Elite09,
            10 => EWaveHudNodeState.Boss10,
            _ => EWaveHudNodeState.Current,
        };
    }

    public bool IsConnectorComplete(
        int currentWave,
        int connectorAfterWave)
    {
        return connectorAfterWave < currentWave;
    }

    public bool IsSupportedWaveCount(int waveCount)
    {
        return waveCount == 10;
    }
}

public class StatusPanel : UIBase
{
    private const int WaveNodeCount = 10;
    private const int WaveConnectorCount = WaveNodeCount - 1;

    [SerializeField] private TextMeshProUGUI playerHpText;
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("Wave Progress")]
    [SerializeField] private Image[] waveNodes;
    [SerializeField] private Image[] waveConnectors;
    [SerializeField] private TextMeshProUGUI[] waveNumberTexts;
    [SerializeField] private Sprite idleNodeSprite;
    [SerializeField] private Sprite currentNodeSprite;
    [SerializeField] private Sprite completeNodeSprite;
    [SerializeField] private Sprite elite05NodeSprite;
    [SerializeField] private Sprite elite09NodeSprite;
    [SerializeField] private Sprite boss10NodeSprite;
    [SerializeField] private Sprite idleConnectorSprite;
    [SerializeField] private Sprite completeConnectorSprite;

    private BattleManager _battleManager;
    private int _maxHp;
    private int _totalWaveCount;
    private bool _isWaveHudValid;
    private readonly WaveHudState _waveHudState = new();

    public override bool IsDefaultPanel => true;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);

        _battleManager = App.Get<BattleManager>();
        _battleManager.OnInitialized += OnBattleInitialized;
        _battleManager.OnWaveChanged += OnWaveChanged;
        _battleManager.OnHpChanged += OnHpChanged;
        _battleManager.OnGoldChanged += OnGoldChanged;

        _isWaveHudValid = ValidateHudReferences();

        _maxHp = _battleManager.playerMaxHp;
        if (_battleManager.IsInitialized)
        {
            OnBattleInitialized();
        }
    }

    private void OnBattleInitialized()
    {
        _totalWaveCount = _battleManager.TotalWaveCount;
        if (!_waveHudState.IsSupportedWaveCount(_totalWaveCount))
        {
            Debug.LogError(
                $"[StatusPanel] Wave HUD requires exactly 10 waves. " +
                $"Loaded: {_totalWaveCount}");
            _isWaveHudValid = false;
        }

        OnWaveChanged(_battleManager.CurrentWaveNumber - 1);
        OnHpChanged(_battleManager.PlayerHp);
        OnGoldChanged(_battleManager.Gold);
    }

    private void OnWaveChanged(int waveIndex)
    {
        if (!_isWaveHudValid) return;

        int currentWave = Mathf.Clamp(
            waveIndex + 1,
            1,
            Mathf.Max(1, _totalWaveCount));
        RefreshWaveProgress(currentWave);
    }

    private void OnHpChanged(int hp)
    {
        playerHpText.text =
            $"{Mathf.Max(0, hp)}/{Mathf.Max(1, _maxHp)}";
    }

    private void OnGoldChanged(int gold)
    {
        goldText.text = Mathf.Max(0, gold).ToString();
    }

    private void RefreshWaveProgress(int currentWave)
    {
        for (int index = 0; index < WaveNodeCount; index++)
        {
            int nodeWave = index + 1;
            waveNodes[index].sprite = GetNodeSprite(
                _waveHudState.ResolveNodeState(currentWave, nodeWave));
            waveNumberTexts[index].text = nodeWave.ToString();
        }

        for (int index = 0; index < WaveConnectorCount; index++)
        {
            int connectorAfterWave = index + 1;
            waveConnectors[index].sprite =
                _waveHudState.IsConnectorComplete(
                    currentWave,
                    connectorAfterWave)
                    ? completeConnectorSprite
                    : idleConnectorSprite;
        }
    }

    private Sprite GetNodeSprite(EWaveHudNodeState state)
    {
        return state switch
        {
            EWaveHudNodeState.Current => currentNodeSprite,
            EWaveHudNodeState.Complete => completeNodeSprite,
            EWaveHudNodeState.Elite05 => elite05NodeSprite,
            EWaveHudNodeState.Elite09 => elite09NodeSprite,
            EWaveHudNodeState.Boss10 => boss10NodeSprite,
            _ => idleNodeSprite,
        };
    }

    private bool ValidateHudReferences()
    {
        bool valid =
            waveNodes != null &&
            waveNodes.Length == WaveNodeCount &&
            waveConnectors != null &&
            waveConnectors.Length == WaveConnectorCount &&
            waveNumberTexts != null &&
            waveNumberTexts.Length == WaveNodeCount;

        if (valid)
        {
            for (int index = 0; index < WaveNodeCount; index++)
            {
                valid &= waveNodes[index] != null;
                valid &= waveNumberTexts[index] != null;
            }

            for (int index = 0; index < WaveConnectorCount; index++)
            {
                valid &= waveConnectors[index] != null;
            }
        }

        valid &= idleNodeSprite != null;
        valid &= currentNodeSprite != null;
        valid &= completeNodeSprite != null;
        valid &= elite05NodeSprite != null;
        valid &= elite09NodeSprite != null;
        valid &= boss10NodeSprite != null;
        valid &= idleConnectorSprite != null;
        valid &= completeConnectorSprite != null;

        if (!valid)
        {
            Debug.LogError(
                "[StatusPanel] Wave HUD requires 10 nodes, " +
                "9 connectors, 10 labels, and all state Sprites.");
        }

        return valid;
    }

    private void OnDestroy()
    {
        if (_battleManager == null) return;

        _battleManager.OnInitialized -= OnBattleInitialized;
        _battleManager.OnWaveChanged -= OnWaveChanged;
        _battleManager.OnHpChanged -= OnHpChanged;
        _battleManager.OnGoldChanged -= OnGoldChanged;
    }
}
