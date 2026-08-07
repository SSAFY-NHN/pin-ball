using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultPanel : UIBase
{
    [Header("Text")]
    [SerializeField] private string victoryTitle = "VICTORY";
    [SerializeField] private string victoryMessage = "모든 웨이브를 클리어했습니다.";
    [SerializeField] private string defeatTitle = "DEFEAT";
    [SerializeField] private string defeatMessage = "방어선이 무너졌습니다.";

    private BattleManager _battleManager;
    private SceneManager _sceneManager;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _messageText;
    private Button _titleButton;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);

        BuildView();

        _battleManager = App.Get<BattleManager>();
        _sceneManager = App.Get<SceneManager>();
        _battleManager.OnStateChanged += OnBattleStateChanged;
        _titleButton.onClick.AddListener(ReturnToTitle);

        OnBattleStateChanged(_battleManager.State);
    }

    private void BuildView()
    {
        var rootRect = transform as RectTransform;
        if (rootRect == null)
        {
            Debug.LogError("[ResultPanel] RectTransform이 필요합니다.");
            return;
        }

        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.SetAsLastSibling();

        var background = gameObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.82f);

        var content = CreateUiObject("Content", transform);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(720f, 420f);

        var contentImage = content.AddComponent<Image>();
        contentImage.color = new Color(0.09f, 0.11f, 0.16f, 0.98f);

        _titleText = CreateText(
            "Title",
            content.transform,
            new Vector2(0f, 100f),
            new Vector2(640f, 100f),
            64f);
        _messageText = CreateText(
            "Message",
            content.transform,
            new Vector2(0f, 5f),
            new Vector2(640f, 80f),
            30f);

        var buttonObject = CreateUiObject("TitleButton", content.transform);
        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -120f);
        buttonRect.sizeDelta = new Vector2(300f, 80f);

        var buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.9f, 0.68f, 0.2f, 1f);
        _titleButton = buttonObject.AddComponent<Button>();
        _titleButton.targetGraphic = buttonImage;

        var buttonText = CreateText(
            "Label",
            buttonObject.transform,
            Vector2.zero,
            new Vector2(300f, 80f),
            30f);
        buttonText.text = "타이틀로";
        buttonText.color = new Color(0.08f, 0.08f, 0.08f);
    }

    private TextMeshProUGUI CreateText(
        string objectName,
        Transform parent,
        Vector2 position,
        Vector2 size,
        float fontSize)
    {
        var textObject = CreateUiObject(objectName, parent);
        var rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = fontSize;
        text.raycastTarget = false;
        return text;
    }

    private GameObject CreateUiObject(string objectName, Transform parent)
    {
        var result = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer));
        result.layer = gameObject.layer;
        result.transform.SetParent(parent, false);
        return result;
    }

    private void OnBattleStateChanged(EWaveState state)
    {
        if (state != EWaveState.Victory && state != EWaveState.Defeat)
        {
            gameObject.SetActive(false);
            return;
        }

        bool isVictory = state == EWaveState.Victory;
        _titleText.text = isVictory ? victoryTitle : defeatTitle;
        _messageText.text = isVictory ? victoryMessage : defeatMessage;
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

        if (_titleButton != null)
        {
            _titleButton.onClick.RemoveListener(ReturnToTitle);
        }
    }
}
