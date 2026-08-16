using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private TextMeshProUGUI playerHpText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI allyCountText;
    [SerializeField] private Color allyCountDefaultColor = Color.white;
    [SerializeField] private Color allyCountOverLimitColor = Color.red;

    [Header("Resource Feedback")]
    [SerializeField] private Color hpFlashColor =
        new(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color goldFlashColor =
        new(1f, 0.82f, 0.2f, 1f);
    [SerializeField, Min(0f)] private float resourceFeedbackDuration = 0.42f;

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
    private StatusFeedbackController _feedbackController;
    private StatusWaveHudController _waveHudController;

    public override bool IsDefaultPanel => true;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);

        _battleManager = App.Get<BattleManager>();
        _battleManager.OnInitialized += OnBattleInitialized;
        _battleManager.OnWaveChanged += OnWaveChanged;
        _battleManager.OnHpChanged += OnHpChanged;
        _battleManager.OnGoldChanged += OnGoldChanged;

        _feedbackController = new StatusFeedbackController(
            playerHpText,
            goldText,
            allyCountText,
            hpFlashColor,
            goldFlashColor,
            resourceFeedbackDuration);
        _waveHudController = new StatusWaveHudController(
            waveNodes,
            waveConnectors,
            idleNodeSprite,
            lockedNodeSprite,
            currentNodeSprite,
            completeNodeSprite,
            elite05NodeSprite,
            elite09NodeSprite,
            boss10NodeSprite,
            idleConnectorSprite,
            completeConnectorSprite);

        _unitManager = App.Get<UnitManager>();
        _unitManager.OnDeployedAllyCountChanged +=
            OnDeployedAllyCountChanged;
        OnDeployedAllyCountChanged(_unitManager.DeployedAllyCount);

        _isWaveHudValid = _waveHudController.ValidateReferences();

        _maxHp = _battleManager.playerMaxHp;
        if (_battleManager.IsInitialized)
        {
            OnBattleInitialized();
        }
    }

    private void OnBattleInitialized()
    {
        _totalWaveCount = _battleManager.TotalWaveCount;
        if (!_waveHudController.SupportsWaveCount(_totalWaveCount))
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
        _waveHudController.Display(currentWave);
    }

    private void OnHpChanged(int hp)
    {
        string value = $"{Mathf.Max(0, hp)}/{Mathf.Max(1, _maxHp)}";
        bool changed = _hasDisplayedHp && playerHpText.text != value;
        playerHpText.text = value;
        _hasDisplayedHp = true;
        if (changed) _feedbackController.EmphasizeHp();
    }

    private void OnGoldChanged(int gold)
    {
        string value = Mathf.Max(0, gold).ToString();
        bool changed = _hasDisplayedGold && goldText.text != value;
        goldText.text = value;
        _hasDisplayedGold = true;
        if (changed) _feedbackController.EmphasizeGold();
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
        _feedbackController?.EmphasizeAllyCount();
    }

    private void OnDestroy()
    {
        _feedbackController?.Clear();

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
