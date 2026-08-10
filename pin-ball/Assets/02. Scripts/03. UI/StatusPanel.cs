using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public enum EWaveHudNodeState
{
    Idle,
    Locked,
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
            return EWaveHudNodeState.Locked;
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
    [SerializeField] private TextMeshProUGUI allyCountText;
    [SerializeField] private Color allyCountDefaultColor = Color.white;
    [SerializeField] private Color allyCountOverLimitColor = Color.red;

    [Header("Wave Progress")]
    [SerializeField] private Image[] waveNodes;
    [SerializeField] private Image[] waveConnectors;
    [SerializeField] private Sprite idleNodeSprite;
    [SerializeField] private Sprite lockedNodeSprite;
    [SerializeField] private Sprite currentNodeSprite;
    [SerializeField] private Sprite completeNodeSprite;
    [SerializeField] private Sprite elite05NodeSprite;
    [SerializeField] private Sprite elite09NodeSprite;
    [SerializeField] private Sprite boss10NodeSprite;
    [SerializeField] private Sprite idleConnectorSprite;
    [SerializeField] private Sprite completeConnectorSprite;

    private BattleManager _battleManager;
    private UnitManager _unitManager;
    private int _maxHp;
    private int _totalWaveCount;
    private bool _isWaveHudValid;
    private bool _hasDisplayedHp;
    private bool _hasDisplayedGold;
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

        _unitManager = App.Get<UnitManager>();
        _unitManager.OnDeployedAllyCountChanged +=
            OnDeployedAllyCountChanged;
        OnDeployedAllyCountChanged(_unitManager.DeployedAllyCount);

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
        string value = $"{Mathf.Max(0, hp)}/{Mathf.Max(1, _maxHp)}";
        bool changed = _hasDisplayedHp && playerHpText.text != value;
        playerHpText.text = value;
        _hasDisplayedHp = true;
        if (changed) Emphasize(playerHpText);
    }

    private void OnGoldChanged(int gold)
    {
        string value = Mathf.Max(0, gold).ToString();
        bool changed = _hasDisplayedGold && goldText.text != value;
        goldText.text = value;
        _hasDisplayedGold = true;
        if (changed) Emphasize(goldText);
    }

    private void OnDeployedAllyCountChanged(int count)
    {
        if (allyCountText == null) return;

        allyCountText.text =
            $"{Mathf.Max(0, count)}/{UnitManager.MaxDeployedAllyCount}";
        allyCountText.color = ShouldWarnAllyCount(count)
            ? allyCountOverLimitColor
            : allyCountDefaultColor;
    }

    public static bool ShouldWarnAllyCount(int count)
    {
        return !UnitManager.CanStartWaveWithAllyCount(count);
    }

    public void EmphasizeAllyCount()
    {
        Emphasize(allyCountText);
    }

    private static void Emphasize(TextMeshProUGUI text)
    {
        if (text == null) return;

        RectTransform rect = text.rectTransform;
        rect.DOKill(true);
        rect.DOShakeAnchorPos(0.3f, 8f, 14, 90f, false, true);
        rect.DOPunchScale(Vector3.one * 0.12f, 0.3f, 6, 0.5f);
    }

    private void RefreshWaveProgress(int currentWave)
    {
        for (int index = 0; index < WaveNodeCount; index++)
        {
            int nodeWave = index + 1;
            waveNodes[index].sprite = GetNodeSprite(
                _waveHudState.ResolveNodeState(currentWave, nodeWave));
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
            EWaveHudNodeState.Locked => lockedNodeSprite,
            _ => idleNodeSprite,
        };
    }

    private bool ValidateHudReferences()
    {
        bool valid =
            waveNodes != null &&
            waveNodes.Length == WaveNodeCount &&
            waveConnectors != null &&
            waveConnectors.Length == WaveConnectorCount;

        if (valid)
        {
            for (int index = 0; index < WaveNodeCount; index++)
            {
                valid &= waveNodes[index] != null;
            }

            for (int index = 0; index < WaveConnectorCount; index++)
            {
                valid &= waveConnectors[index] != null;
            }
        }

        valid &= idleNodeSprite != null;
        valid &= lockedNodeSprite != null;
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
                "9 connectors, standard-wave labels, and all state Sprites.");
        }

        return valid;
    }

    private void OnDestroy()
    {
        playerHpText?.rectTransform.DOKill();
        goldText?.rectTransform.DOKill();
        allyCountText?.rectTransform.DOKill();

        if (_battleManager != null)
        {
            _battleManager.OnInitialized -= OnBattleInitialized;
            _battleManager.OnWaveChanged -= OnWaveChanged;
            _battleManager.OnHpChanged -= OnHpChanged;
            _battleManager.OnGoldChanged -= OnGoldChanged;
        }

        if (_unitManager != null)
        {
            _unitManager.OnDeployedAllyCountChanged -=
                OnDeployedAllyCountChanged;
        }
    }
}
