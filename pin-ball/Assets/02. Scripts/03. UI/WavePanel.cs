using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WavePanel : UIBase
{
    public override bool IsDefaultPanel => true;
    
    [SerializeField] private Button startButton;
    [SerializeField] private Button launchButton;
    [SerializeField] private TextMeshProUGUI feedbackText;
    
    private BattleManager _battleManager;
    private PinballManager _pinballManager;
    private UnitManager _unitManager;
    private EPinballState _pinballState = EPinballState.Idle;
    
    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);

        _battleManager = App.Get<BattleManager>();
        _battleManager.OnStateChanged += OnBattleStateChanged;
        _battleManager.OnActionRejected += OnActionRejected;

        _unitManager = App.Get<UnitManager>();
        
        _pinballManager = App.Get<PinballManager>();
        _pinballManager.OnStateChanged += OnPinballStateChanged;

        startButton.onClick.AddListener(_battleManager.StartWave);
        launchButton.onClick.AddListener(_pinballManager.LaunchBall);

        EnsureFeedbackText();
        OnBattleStateChanged(_battleManager.State);
    }
    
    private void OnBattleStateChanged(EWaveState state)
    {
        if (state != EWaveState.Pending && feedbackText != null)
        {
            feedbackText.text = string.Empty;
        }

        RefreshButtons();
    }
    
    private void OnPinballStateChanged(EPinballState state)
    {
        _pinballState = state;
        RefreshButtons();
    }

    private void OnActionRejected(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }

    private void RefreshButtons()
    {
        if (_battleManager == null) return;

        bool isPreparation = _battleManager.IsPreparationPhase;
        bool hasAlly =
            _unitManager != null &&
            _unitManager.RemainingAllyCount > 0;

        if (startButton != null)
        {
            startButton.gameObject.SetActive(isPreparation);
            startButton.interactable =
                isPreparation &&
                _pinballState == EPinballState.Idle &&
                hasAlly;
        }

        if (launchButton != null)
        {
            launchButton.interactable = isPreparation;
        }

        if (feedbackText != null && isPreparation)
        {
            const string emptyRosterMessage =
                "아군 유닛을 한 명 이상 준비해야 합니다.";
            if (!hasAlly)
            {
                feedbackText.text = emptyRosterMessage;
            }
            else if (feedbackText.text == emptyRosterMessage)
            {
                feedbackText.text = string.Empty;
            }
        }
    }

    private void EnsureFeedbackText()
    {
        if (feedbackText != null) return;

        var feedbackObject = new GameObject(
            "StartFeedback",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        feedbackObject.layer = gameObject.layer;
        feedbackObject.transform.SetParent(transform, false);

        var rectTransform = feedbackObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = new Vector2(0f, 72f);
        rectTransform.sizeDelta = new Vector2(520f, 60f);

        feedbackText = feedbackObject.GetComponent<TextMeshProUGUI>();
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.color = new Color(1f, 0.35f, 0.35f);
        feedbackText.fontSize = 28f;
        feedbackText.raycastTarget = false;
    }

    private void OnDestroy()
    {
        if (_battleManager != null)
        {
            _battleManager.OnStateChanged -= OnBattleStateChanged;
            _battleManager.OnActionRejected -= OnActionRejected;
        }

        if (_pinballManager != null)
        {
            _pinballManager.OnStateChanged -= OnPinballStateChanged;
        }

        if (startButton != null && _battleManager != null)
        {
            startButton.onClick.RemoveListener(_battleManager.StartWave);
        }

        if (launchButton != null && _pinballManager != null)
        {
            launchButton.onClick.RemoveListener(_pinballManager.LaunchBall);
        }
    }
}
