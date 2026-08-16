using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(BoxCollider2D))]
public class PinballGoal : MonoBehaviour
{
    [SerializeField] private BattleUnitSpawnData unitData = new()
    {
        UnitId = "warrior",
        Level = 1
    };

    public BattleUnitSpawnData UnitData => unitData;

    [Header("VFX")]
    [SerializeField] private Material effectMaterial;
    [SerializeField] private ArcaneSpriteEffect runeBurst;
    [SerializeField] private ArcaneSpriteEffect absorptionRing;
    [SerializeField] private ArcaneSpriteEffect goalBurst;
    [SerializeField] private ArcaneSpriteEffect arcTopLeft;
    [SerializeField] private ArcaneSpriteEffect arcTopRight;
    [SerializeField] private ArcaneSpriteEffect arcBottomLeft;
    [SerializeField] private ArcaneSpriteEffect arcBottomRight;
    [SerializeField] private ArcaneSpriteEffect goalSpark;

    private PinballManager _pinballManager;
    private BoxCollider2D _collider;
    private float _baseWidth;
    private ArcaneMaskGlowController _runeGlow;
    private Transform _runeTransform;
    private ArcaneSpriteEffect[] _goalEffects;
    private BattleCameraController _cameraFeedback;

    private void Awake()
    {
        if (unitData == null)
        {
            unitData = new BattleUnitSpawnData();
        }
        else if (unitData.UnitId == "DefaultAlly")
        {
            unitData.UnitId = "warrior";
        }

        _collider = GetComponent<BoxCollider2D>();
        _collider.isTrigger = true;
        _baseWidth = _collider.size.x;
        InitializeVfx();
    }

    private void Start()
    {
        _pinballManager = App.Get<PinballManager>();
        _pinballManager.RegisterGoal(this);
    }

    private void OnDestroy()
    {
        _pinballManager?.UnregisterGoal(this);
        _runeTransform?.DOKill();
    }

    private void OnMouseDown()
    {
        _pinballManager?.SelectGoal(this);
    }

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1))
        {
            _pinballManager?.SelectSwapGoal(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var ball = other.GetComponentInParent<Pinball>();
        if (ball == null) return;

        PlayGoalEffect(ball.transform.position);
        _pinballManager.OnGoalBall(ball, this);
    }

    internal void SetWidthMultiplier(float multiplier, float maxWorldWidth)
    {
        var scaleX = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.x));
        var targetWorldWidth = _baseWidth * scaleX * multiplier;

        if (maxWorldWidth < float.MaxValue)
        {
            targetWorldWidth = Mathf.Min(targetWorldWidth, maxWorldWidth);
        }

        var size = _collider.size;
        size.x = targetWorldWidth / scaleX;
        _collider.size = size;
    }

    internal void SetUnitData(BattleUnitSpawnData data)
    {
        unitData = data;
    }

    private void InitializeVfx()
    {
        var catalog = ArcaneVfxCatalog.Load();
        if (catalog == null) return;

        _runeTransform = transform.Find("Rune");
        _runeGlow = _runeTransform?
            .GetComponent<ArcaneMaskGlowController>();

        _cameraFeedback = Camera.main != null
            ? Camera.main.GetComponent<BattleCameraController>()
            : null;
        var runeRenderer = _runeTransform != null
            ? _runeTransform.GetComponent<SpriteRenderer>()
            : null;
        if (runeRenderer != null)
        {
            runeBurst?.Initialize(
                new[] { runeRenderer.sprite },
                effectMaterial,
                25);
        }
        absorptionRing?.Initialize(catalog.ballRing, effectMaterial, 24);
        goalBurst?.Initialize(catalog.ballRing, effectMaterial, 25);
        arcTopLeft?.Initialize(catalog.goalArcTopLeft, effectMaterial, 23);
        arcTopRight?.Initialize(catalog.goalArcTopRight, effectMaterial, 23);
        arcBottomLeft?.Initialize(catalog.goalArcBottomLeft, effectMaterial, 23);
        arcBottomRight?.Initialize(catalog.goalArcBottomRight, effectMaterial, 23);
        goalSpark?.Initialize(catalog.goalSpark, effectMaterial, 24);
        _goalEffects = new[]
        {
            arcTopLeft,
            arcTopRight,
            arcBottomLeft,
            arcBottomRight,
            goalSpark
        };
    }

    private void PlayGoalEffect(Vector2 ballPosition)
    {
        _runeGlow?.Pulse(2.5f, 0.35f);
        _cameraFeedback?.PlayPinballGoalShake();
        if (_runeTransform != null)
        {
            _runeTransform.DOKill(true);
            _runeTransform.DOPunchScale(
                Vector3.one * 0.18f,
                0.32f,
                5,
                0.45f);
        }
        if (_goalEffects == null) return;

        var position = new Vector3(transform.position.x, transform.position.y + 0.2f, transform.position.z);
        var runePosition = _runeTransform != null
            ? _runeTransform.position
            : position;
        runeBurst?.Play(
            runePosition,
            0.42f,
            Vector3.one * 0.42f,
            Vector3.one * 0.78f,
            new Color(0.25f, 0.9f, 1f, 1f));
        absorptionRing?.Play(
            ballPosition,
            runePosition,
            0.22f,
            Vector3.one * 1.15f,
            Vector3.one * 0.18f,
            new Color(0.55f, 0.12f, 1f, 1f));
        goalBurst?.Play(
            runePosition,
            0.38f,
            Vector3.one * 0.25f,
            Vector3.one * 1.65f,
            new Color(0.03f, 0.75f, 1f, 1f));
        var arcColor = new Color(0.03f, 0.65f, 1f, 0.9f);
        for (var index = 0; index < _goalEffects.Length - 1; index++)
        {
            _goalEffects[index]?.Play(
                position,
                0.38f,
                Vector3.one * 0.5f,
                Vector3.one * 0.85f,
                arcColor);
        }

        _goalEffects[^1]?.Play(
            ballPosition,
            0.28f,
            Vector3.one * 0.25f,
            Vector3.one * 0.75f,
            new Color(0.55f, 0.12f, 1f, 1f));
    }
}
