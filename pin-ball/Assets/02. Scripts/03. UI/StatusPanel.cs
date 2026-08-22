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
    public EWaveHudNodeState ResolveNodeState(int currentWave, int nodeWave)
    {
        if (nodeWave < currentWave) return EWaveHudNodeState.Complete;
        if (nodeWave > currentWave) return EWaveHudNodeState.Locked;
        return nodeWave switch
        {
            5 => EWaveHudNodeState.Elite05,
            9 => EWaveHudNodeState.Elite09,
            10 => EWaveHudNodeState.Boss10,
            _ => EWaveHudNodeState.Current
        };
    }

    public bool IsConnectorComplete(int currentWave, int connectorAfterWave) =>
        connectorAfterWave < currentWave;

    public bool IsSupportedWaveCount(int waveCount) => waveCount == 10;
}

public class StatusPanel : UIBase
{
    [SerializeField] private TextMeshProUGUI playerHpText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI defenseLineText;

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

    [Header("Resource Feedback")]
    [SerializeField] private Color hpFlashColor =
        new(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color goldFlashColor =
        new(1f, 0.82f, 0.2f, 1f);
    [SerializeField, Min(0f)] private float resourceFeedbackDuration = 0.42f;

    private BattleManager battleManager;
    private bool hasDisplayedHp;
    private bool hasDisplayedGold;
    private int allyDefenseCurrent;
    private int allyDefenseMaximum;
    private int enemyDefenseCurrent;
    private int enemyDefenseMaximum;
    private bool hasDisplayedDefenseLine;
    private StatusFeedbackController feedbackController;
    private StatusWaveHudController waveHudController;
    private bool isWaveHudValid;

    public override bool IsDefaultPanel => true;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);

        battleManager = App.Get<BattleManager>();
        battleManager.OnInitialized += OnBattleInitialized;
        battleManager.OnWaveChanged += OnWaveChanged;
        battleManager.OnHpChanged += OnHpChanged;
        battleManager.OnGoldChanged += OnGoldChanged;
        battleManager.OnDefenseLineHpChanged += OnDefenseLineHpChanged;

        feedbackController = new StatusFeedbackController(
            playerHpText,
            goldText,
            defenseLineText,
            hpFlashColor,
            goldFlashColor,
            resourceFeedbackDuration);

        waveHudController = new StatusWaveHudController(
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
        isWaveHudValid = waveHudController.ValidateReferences();

        if (battleManager.IsInitialized) OnBattleInitialized();
    }

    private void OnBattleInitialized()
    {
        if (!waveHudController.SupportsWaveCount(battleManager.TotalWaveCount))
        {
            Debug.LogError(
                $"[StatusPanel] Wave HUD requires exactly 10 waves. " +
                $"Loaded: {battleManager.TotalWaveCount}");
            isWaveHudValid = false;
        }

        OnWaveChanged(battleManager.CurrentWaveNumber);
        OnHpChanged(battleManager.PlayerHp);
        OnGoldChanged(battleManager.Gold);
        OnDefenseLineHpChanged(
            EBattleTeam.Ally,
            battleManager.GetDefenseLineHp(EBattleTeam.Ally),
            battleManager.GetDefenseLineMaximumHp(EBattleTeam.Ally));
        OnDefenseLineHpChanged(
            EBattleTeam.Enemy,
            battleManager.GetDefenseLineHp(EBattleTeam.Enemy),
            battleManager.GetDefenseLineMaximumHp(EBattleTeam.Enemy));
    }

    private void OnWaveChanged(int wave)
    {
        if (!isWaveHudValid) return;
        waveHudController.Display(Mathf.Clamp(wave, 1, 10));
    }

    private void OnHpChanged(int hp)
    {
        if (playerHpText == null) return;

        string value = FormatChances(hp, battleManager.MaximumPlayerHp);
        bool changed = hasDisplayedHp && playerHpText.text != value;
        playerHpText.text = value;
        hasDisplayedHp = true;
        if (changed) feedbackController.EmphasizeHp();
    }

    private void OnDefenseLineHpChanged(
        EBattleTeam team,
        int current,
        int maximum)
    {
        if (team == EBattleTeam.Ally)
        {
            allyDefenseCurrent = current;
            allyDefenseMaximum = maximum;
        }
        else
        {
            enemyDefenseCurrent = current;
            enemyDefenseMaximum = maximum;
        }

        if (defenseLineText == null) return;
        string value = FormatDefenseLines(
            allyDefenseCurrent,
            allyDefenseMaximum,
            enemyDefenseCurrent,
            enemyDefenseMaximum);
        bool changed = hasDisplayedDefenseLine && defenseLineText.text != value;
        defenseLineText.text = value;
        hasDisplayedDefenseLine = true;
        if (changed) feedbackController.EmphasizeDefenseLine();
    }

    public static string FormatChances(int current, int maximum) =>
        $"기회 {Mathf.Max(0, current)}/{Mathf.Max(1, maximum)}";

    public static string FormatDefenseLines(
        int allyCurrent,
        int allyMaximum,
        int enemyCurrent,
        int enemyMaximum) =>
        $"아군 {Mathf.Max(0, allyCurrent)}/{Mathf.Max(1, allyMaximum)} | " +
        $"적 {Mathf.Max(0, enemyCurrent)}/{Mathf.Max(1, enemyMaximum)}";

    private void OnGoldChanged(int gold)
    {
        if (goldText == null) return;

        string value = Mathf.Max(0, gold).ToString();
        bool changed = hasDisplayedGold && goldText.text != value;
        goldText.text = value;
        hasDisplayedGold = true;
        if (changed) feedbackController.EmphasizeGold();
    }

    private void OnDestroy()
    {
        feedbackController?.Clear();
        if (battleManager == null) return;

        battleManager.OnInitialized -= OnBattleInitialized;
        battleManager.OnWaveChanged -= OnWaveChanged;
        battleManager.OnHpChanged -= OnHpChanged;
        battleManager.OnGoldChanged -= OnGoldChanged;
        battleManager.OnDefenseLineHpChanged -= OnDefenseLineHpChanged;
    }
}
