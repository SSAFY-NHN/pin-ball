using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialManager : MonoBehaviour
{
    public const string CompletionKey = "Tutorial.Completed";

    [Header("Tutorial UI")]
    [SerializeField] private GameObject overlay;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private TutorialFocusIndicator focusIndicator;

    [Header("Game UI")]
    [SerializeField] private BottomTabPanel bottomTabPanel;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button itemsButton;
    [SerializeField] private Button waveStartButton;
    [SerializeField] private Transform goalFocusTarget;
    [SerializeField] private Transform magnetFocusTarget;
    [SerializeField] private Transform launcherFocusTarget;
    [SerializeField, Min(30f)] private float maximumDuration = 120f;

    private TutorialProgress _progress;
    private BattleManager _battleManager;
    private PinballManager _pinballManager;
    private UnitManager _unitManager;
    private ItemManager _itemManager;
    private ShopPanel _shopPanel;
    private WavePanel _wavePanel;
    private float _startedAt;
    private string _firstUnitId;
    private bool _initialized;
    private bool _isCompleting;

    private void Start()
    {
        if (_initialized) return;
#if UNITY_EDITOR
        PlayerPrefs.DeleteKey(CompletionKey);
#endif
        if (PlayerPrefs.GetInt(CompletionKey, 0) != 0)
        {
            if (overlay != null) overlay.SetActive(false);
            focusIndicator?.Hide();
            enabled = false;
            return;
        }

        _initialized = true;
        _battleManager = App.Get<BattleManager>();
        _pinballManager = App.Get<PinballManager>();
        _unitManager = App.Get<UnitManager>();
        _itemManager = App.Get<ItemManager>();
        _shopPanel = FindFirstObjectByType<ShopPanel>();
        _wavePanel = FindFirstObjectByType<WavePanel>();
        _progress = new TutorialProgress();
        _startedAt = Time.unscaledTime;
        _battleManager.AddGold(_pinballManager.CurrentLaunchCost * 3);

        continueButton.onClick.AddListener(OnContinue);
        skipButton.onClick.AddListener(CompleteTutorial);
        bottomTabPanel.OnTabChanged += OnTabChanged;
        _pinballManager.OnGoalReached += OnGoalReached;
        _unitManager.OnAlliesMerged += OnAlliesMerged;
        _itemManager.OnItemPurchased += OnItemPurchased;
        _battleManager.OnStateChanged += OnBattleStateChanged;

        ShowCurrentStep();
    }

    private void Update()
    {
        if (_progress != null &&
            Time.unscaledTime - _startedAt >= maximumDuration)
        {
            CompleteTutorial();
        }
    }

    private void OnContinue()
    {
        _progress.ContinueFromMessage();
        ShowCurrentStep();
    }

    private void OnGoalReached(BattleUnitSpawnData _)
    {
        if (_progress.Step == TutorialStep.FirstLaunch)
        {
            _firstUnitId = _.UnitId;
        }
        else if (_progress.Step == TutorialStep.SecondLaunch &&
                 !string.IsNullOrEmpty(_firstUnitId))
        {
            _.UnitId = _firstUnitId;
        }
        _progress.NotifyGoalReached();
        ShowCurrentStep();
    }

    private void OnAlliesMerged(int _)
    {
        if (_progress.Step != TutorialStep.Merge) return;
        ShowMessage("같은 유닛을 합치면 레벨이 오릅니다.\n5레벨이 되면 자동 전직합니다.");
    }

    private void OnTabChanged(BottomPanelTab tab)
    {
        if (tab != BottomPanelTab.Items) return;
        _progress.NotifyItemsOpened();
        ShowCurrentStep();
    }

    private void OnItemPurchased(Item item)
    {
        if (item == null || item.Key != EItem.PersonalHealingPotion) return;
        _shopPanel?.SetTutorialPurchaseRestriction(null);
        _progress.NotifyPersonalPotionPurchased();
        ShowCurrentStep();
    }

    private void OnBattleStateChanged(EWaveState state)
    {
        if (state != EWaveState.Active) return;
        _progress.NotifyWaveStarted();
        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        if (_progress.Step == TutorialStep.Complete)
        {
            CompleteTutorial();
            return;
        }

        switch (_progress.Step)
        {
            case TutorialStep.GoalExplanation:
                ShowMessage(
                    "골인 지점마다 소환되는 유닛이 다릅니다.\n원하는 유닛의 골인 지점을 노려보세요.",
                    goalFocusTarget);
                break;
            case TutorialStep.MagnetExplanation:
                ShowMessage(
                    "공이 자석 가까이 올 때 자석을 누르면\n공의 궤도를 끌어당길 수 있습니다.",
                    magnetFocusTarget);
                break;
            case TutorialStep.FirstLaunch:
                ShowAction(
                    "합성할 같은 유닛 2마리를 준비합니다. (0/2)\n발사대를 당겨 첫 유닛을 소환하세요.",
                    null,
                    launcherFocusTarget);
                break;
            case TutorialStep.SecondLaunch:
                ShowAction(
                    "같은 유닛이 한 마리 더 필요합니다. (1/2)\n발사대를 다시 당겨 소환하세요.",
                    null,
                    launcherFocusTarget);
                break;
            case TutorialStep.Merge:
                Transform allyTarget = _unitManager.OwnedAllies.Count > 0
                    ? _unitManager.OwnedAllies[0].transform
                    : null;
                ShowAction(
                    "같은 유닛 2마리가 준비됐습니다. (2/2)\n한 유닛을 다른 유닛 위에 겹쳐 합성하세요.",
                    null,
                    allyTarget);
                break;
            case TutorialStep.BuyPersonalPotion:
                _shopPanel?.SetTutorialPurchaseRestriction(EItem.PersonalHealingPotion);
                if (bottomTabPanel.CurrentTab != BottomPanelTab.Shop)
                {
                    bottomTabPanel.ShowShop();
                }
                ShopSlot potionSlot = _shopPanel?.FindSlot(EItem.PersonalHealingPotion);
                ShowAction(
                    "상점에서 개인 회복 포션을 하나 구매하세요.",
                    potionSlot != null ? potionSlot.PurchaseButton : null,
                    potionSlot != null ? potionSlot.transform : null);
                break;
            case TutorialStep.Items:
                ShowAction(
                    "아이템 탭을 눌러 구매한 포션을 확인하세요.",
                    itemsButton,
                    itemsButton.transform);
                break;
            case TutorialStep.StartWave:
                ShowAction(
                    "준비가 끝났습니다. 웨이브 시작을 눌러 전투를 시작하세요.",
                    waveStartButton,
                    waveStartButton.transform);
                break;
        }
    }

    private void ShowMessage(string message, Transform focusTarget = null)
    {
        overlay.SetActive(true);
        messageText.text = message;
        continueButton.gameObject.SetActive(true);
        SetOnlyTargetInteractable(null);
        focusIndicator?.SetInputBlocked(true);
        Focus(focusTarget);
    }

    private void ShowAction(
        string message,
        Button targetButton = null,
        Transform focusTarget = null)
    {
        overlay.SetActive(true);
        messageText.text = message;
        continueButton.gameObject.SetActive(false);
        SetOnlyTargetInteractable(targetButton);
        focusIndicator?.SetInputBlocked(false);
        Focus(focusTarget);
    }

    private void Focus(Transform target)
    {
        if (target == null) focusIndicator?.Hide();
        else focusIndicator?.Focus(target, new Vector2(24f, 20f));
    }

    private void SetOnlyTargetInteractable(Button target)
    {
        if (shopButton != null) shopButton.interactable = target == shopButton;
        if (itemsButton != null) itemsButton.interactable = target == itemsButton;
        if (waveStartButton != null) waveStartButton.interactable = target == waveStartButton;
    }

    private void CompleteTutorial()
    {
        if (_isCompleting) return;
        _isCompleting = true;
        Unsubscribe();
        _shopPanel?.SetTutorialPurchaseRestriction(null);
        focusIndicator?.Hide();
        focusIndicator?.SetInputBlocked(false);
        bottomTabPanel?.RefreshCurrentTab();
        _wavePanel?.RefreshTutorialState();
        PlayerPrefs.SetInt(CompletionKey, 1);
        PlayerPrefs.Save();
        if (overlay != null) overlay.SetActive(false);
        enabled = false;
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinue);
        if (skipButton != null) skipButton.onClick.RemoveListener(CompleteTutorial);
        if (bottomTabPanel != null) bottomTabPanel.OnTabChanged -= OnTabChanged;
        if (_pinballManager != null) _pinballManager.OnGoalReached -= OnGoalReached;
        if (_unitManager != null) _unitManager.OnAlliesMerged -= OnAlliesMerged;
        if (_itemManager != null) _itemManager.OnItemPurchased -= OnItemPurchased;
        if (_battleManager != null) _battleManager.OnStateChanged -= OnBattleStateChanged;
    }
}
