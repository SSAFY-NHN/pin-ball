using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ResultPanel : UIBase
{
    [Header("Text")]
    [SerializeField] private string victoryTitle = "VICTORY";
    [SerializeField] private string victoryMessage = "모든 웨이브를 클리어했습니다.";
    [SerializeField] private string defeatTitle = "DEFEAT";
    [SerializeField] private string defeatMessage = "방어선이 무너졌습니다.";
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button titleButton;

    private BattleManager _battleManager;
    private SceneManager _sceneManager;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);

        if (titleText == null || messageText == null || titleButton == null)
        {
            Debug.LogError("[ResultPanel] UI 참조가 설정되지 않았습니다.");
            enabled = false;
            return;
        }

        _battleManager = App.Get<BattleManager>();
        _sceneManager = App.Get<SceneManager>();
        _battleManager.OnStateChanged += OnBattleStateChanged;
        titleButton.onClick.AddListener(ReturnToTitle);

        OnBattleStateChanged(_battleManager.State);
    }

    private void OnBattleStateChanged(EWaveState state)
    {
        if (state != EWaveState.Victory && state != EWaveState.Defeat)
        {
            gameObject.SetActive(false);
            return;
        }

        bool isVictory = state == EWaveState.Victory;
        titleText.text = isVictory ? victoryTitle : defeatTitle;
        messageText.text = isVictory ? victoryMessage : defeatMessage;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    private void ReturnToTitle()
    {
        _sceneManager?.Load(ESceneName.Title);
    }

    private void OnDestroy()
    {
        if (_battleManager != null)
        {
            _battleManager.OnStateChanged -= OnBattleStateChanged;
        }

        if (titleButton != null)
        {
            titleButton.onClick.RemoveListener(ReturnToTitle);
        }
    }
}
