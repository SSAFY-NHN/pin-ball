using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Serialized shell retained until the class and script cleanup passes.
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
    [SerializeField] private ShopPanel shopPanel;
    [SerializeField] private WavePanel wavePanel;
    [SerializeField] private Transform goalFocusTarget;
    [SerializeField] private Transform magnetFocusTarget;
    [SerializeField] private Transform launcherFocusTarget;
    [SerializeField, Min(30f)] private float maximumDuration = 120f;

    private TutorialProgress _progress;
    private BattleManager _battleManager;
    private PinballManager _pinballManager;
    private UnitManager _unitManager;
    private ItemManager _itemManager;
    private TutorialUIController _uiController;
    private TutorialInteractionController _interactionController;
    private TutorialGameRuleController _gameRuleController;
    private bool _initialized;
    private bool _isCompleting;
}
