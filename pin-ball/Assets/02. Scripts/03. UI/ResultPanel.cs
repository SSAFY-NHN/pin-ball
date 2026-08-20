using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultPanel : UIBase
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button titleButton;

    private BattleManager _battleManager;
    private bool _isShown;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);
        _battleManager = App.Get<BattleManager>();
        _battleManager.OnRunEnded += ShowGameOver;
        restartButton.onClick.AddListener(RestartGame);
        titleButton.onClick.AddListener(LoadTitle);
        gameObject.SetActive(false);
    }

    private void ShowGameOver(int reachedWave)
    {
        if (_isShown) return;

        _isShown = true;
        messageText.text = $"GAME OVER\n도달 웨이브 {reachedWave}";
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    private static void RestartGame()
    {
        App.Get<SceneManager>().Load(ESceneName.Game);
    }

    private static void LoadTitle()
    {
        App.Get<SceneManager>().Load(ESceneName.Title);
    }

    private void OnDestroy()
    {
        if (_battleManager != null) _battleManager.OnRunEnded -= ShowGameOver;
        if (restartButton != null) restartButton.onClick.RemoveListener(RestartGame);
        if (titleButton != null) titleButton.onClick.RemoveListener(LoadTitle);
    }
}
