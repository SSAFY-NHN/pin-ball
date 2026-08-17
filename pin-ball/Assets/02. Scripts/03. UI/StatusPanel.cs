using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusPanel : UIBase
{
    [SerializeField] private TextMeshProUGUI playerHpText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private Image[] waveNodes;
    [SerializeField] private Image[] waveConnectors;

    [Header("Resource Feedback")]
    [SerializeField] private Color hpFlashColor =
        new(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color goldFlashColor =
        new(1f, 0.82f, 0.2f, 1f);
    [SerializeField, Min(0f)] private float resourceFeedbackDuration = 0.42f;

    private BattleManager battleManager;
    private bool hasDisplayedHp;
    private bool hasDisplayedGold;
    private StatusFeedbackController feedbackController;

    public override bool IsDefaultPanel => true;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);

        battleManager = App.Get<BattleManager>();
        battleManager.OnInitialized += OnBattleInitialized;
        battleManager.OnWaveChanged += OnStageChanged;
        battleManager.OnHpChanged += OnHpChanged;
        battleManager.OnGoldChanged += OnGoldChanged;

        feedbackController = new StatusFeedbackController(
            playerHpText,
            goldText,
            stageText,
            hpFlashColor,
            goldFlashColor,
            resourceFeedbackDuration);

        HideLegacyWaveProgress();

        if (battleManager.IsInitialized) OnBattleInitialized();
    }

    private void HideLegacyWaveProgress()
    {
        if (waveNodes != null)
        {
            foreach (var node in waveNodes)
            {
                if (node != null) node.gameObject.SetActive(false);
            }
        }

        if (waveConnectors != null)
        {
            foreach (var connector in waveConnectors)
            {
                if (connector != null) connector.gameObject.SetActive(false);
            }
        }
    }

    private void OnBattleInitialized()
    {
        OnStageChanged(battleManager.CurrentStageNumber);
        OnHpChanged(battleManager.PlayerHp);
        OnGoldChanged(battleManager.Gold);
    }

    private void OnStageChanged(int stage)
    {
        if (stageText != null)
        {
            stageText.text = $"단계 {Mathf.Max(1, stage)}";
        }
    }

    private void OnHpChanged(int hp)
    {
        if (playerHpText == null) return;

        string value =
            $"{Mathf.Max(0, hp)}/{Mathf.Max(1, battleManager.MaximumPlayerHp)}";
        bool changed = hasDisplayedHp && playerHpText.text != value;
        playerHpText.text = value;
        hasDisplayedHp = true;
        if (changed) feedbackController.EmphasizeHp();
    }

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
        battleManager.OnWaveChanged -= OnStageChanged;
        battleManager.OnHpChanged -= OnHpChanged;
        battleManager.OnGoldChanged -= OnGoldChanged;
    }
}
