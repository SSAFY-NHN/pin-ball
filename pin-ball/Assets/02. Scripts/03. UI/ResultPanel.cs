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
    private SceneManager _sceneManager;

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
        overlayImage.sprite = isVictory
            ? victoryOverlaySprite
            : defeatOverlaySprite;
        titleImage.sprite = isVictory ? victoryTitleSprite : defeatTitleSprite;
        iconImage.sprite = isVictory ? victoryIconSprite : defeatIconSprite;
        buttonAccentImage.sprite = isVictory
            ? victoryButtonAccentSprite
            : defeatButtonAccentSprite;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    private void ReturnToTitle()
    {
        _sceneManager?.Load(ESceneName.Title);
    }

    private bool ValidateReferences()
    {
        bool valid =
            titleText != null &&
            messageText != null &&
            titleButton != null &&
            overlayImage != null &&
            titleImage != null &&
            iconImage != null &&
            buttonAccentImage != null &&
            victoryOverlaySprite != null &&
            defeatOverlaySprite != null &&
            victoryTitleSprite != null &&
            defeatTitleSprite != null &&
            victoryIconSprite != null &&
            defeatIconSprite != null &&
            victoryButtonAccentSprite != null &&
            defeatButtonAccentSprite != null;

        if (!valid)
        {
            Debug.LogError("[ResultPanel] 결과 UI 이미지 참조가 설정되지 않았습니다.");
        }

        return valid;
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
