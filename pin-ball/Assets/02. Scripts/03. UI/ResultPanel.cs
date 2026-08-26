using TMPro;
using UnityEngine;
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
    [SerializeField] private Button restartButton;
    [SerializeField] private Button titleButton;

    [Header("Artwork")]
    [SerializeField] private Image overlayImage;
    [SerializeField] private Image titleImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image buttonAccentImage;
    [SerializeField] private Sprite victoryOverlaySprite;
    [SerializeField] private Sprite defeatOverlaySprite;
    [SerializeField] private Sprite victoryTitleSprite;
    [SerializeField] private Sprite defeatTitleSprite;
    [SerializeField] private Sprite victoryIconSprite;
    [SerializeField] private Sprite defeatIconSprite;
    [SerializeField] private Sprite victoryButtonAccentSprite;
    [SerializeField] private Sprite defeatButtonAccentSprite;

    private BattleManager _battleManager;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);
        if (!ValidateReferences())
        {
            Debug.LogError("[ResultPanel] UI 참조가 설정되지 않았습니다.");
            enabled = false;
            return;
        }

        _battleManager = App.Get<BattleManager>();
        _battleManager.OnStateChanged += OnBattleStateChanged;
        restartButton.onClick.AddListener(RestartGame);
        titleButton.onClick.AddListener(LoadTitle);
        OnBattleStateChanged(_battleManager.State);
    }

    private void OnBattleStateChanged(EWaveState state)
    {
        if (state is not EWaveState.Victory and not EWaveState.Defeat)
        {
            gameObject.SetActive(false);
            return;
        }

        bool victory = state == EWaveState.Victory;
        titleText.text = victory ? victoryTitle : defeatTitle;
        messageText.text = victory ? victoryMessage : defeatMessage;
        overlayImage.sprite = victory
            ? victoryOverlaySprite
            : defeatOverlaySprite;
        titleImage.sprite = victory ? victoryTitleSprite : defeatTitleSprite;
        iconImage.sprite = victory ? victoryIconSprite : defeatIconSprite;
        buttonAccentImage.sprite = victory
            ? victoryButtonAccentSprite
            : defeatButtonAccentSprite;
        App.Get<PinballManager>().PauseForResult();
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    private bool ValidateReferences()
    {
        return titleText != null && messageText != null &&
               restartButton != null && titleButton != null &&
               overlayImage != null && titleImage != null &&
               iconImage != null && buttonAccentImage != null &&
               victoryOverlaySprite != null && defeatOverlaySprite != null &&
               victoryTitleSprite != null && defeatTitleSprite != null &&
               victoryIconSprite != null && defeatIconSprite != null &&
               victoryButtonAccentSprite != null &&
               defeatButtonAccentSprite != null;
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
        if (_battleManager != null)
        {
            _battleManager.OnStateChanged -= OnBattleStateChanged;
        }
        if (restartButton != null) restartButton.onClick.RemoveListener(RestartGame);
        if (titleButton != null) titleButton.onClick.RemoveListener(LoadTitle);
    }
}
