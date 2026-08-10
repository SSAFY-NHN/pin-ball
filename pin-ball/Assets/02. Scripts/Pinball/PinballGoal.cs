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

    private PinballManager _pinballManager;
    private BoxCollider2D _collider;
    private float _baseWidth;
    private ArcaneMaskGlowController _runeGlow;
    private Transform _runeTransform;
    private ArcaneSpriteEffect[] _goalEffects;
    private ArcaneSpriteEffect _runeBurst;
    private ArcaneSpriteEffect _absorptionRing;
    private ArcaneSpriteEffect _goalBurst;
    private Material _effectMaterial;
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
        if (_effectMaterial != null) Destroy(_effectMaterial);
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

        var shader = Resources.Load<Shader>("ArcaneVFX/ArcaneAdditive");
        if (shader == null) return;

        _effectMaterial = new Material(shader)
        {
            name = "Arcane Goal VFX (Runtime)",
            hideFlags = HideFlags.HideAndDontSave
        };
        _effectMaterial.SetFloat("_Intensity", 1.8f);
        _effectMaterial.SetFloat("_GlowSpread", 1.25f);
        _cameraFeedback = Camera.main != null
            ? Camera.main.GetComponent<BattleCameraController>()
            : null;
        var runeRenderer = _runeTransform != null
            ? _runeTransform.GetComponent<SpriteRenderer>()
            : null;
        if (runeRenderer != null)
        {
            _runeBurst = CreateEffect(
                "Goal Rune Burst",
                new[] { runeRenderer.sprite },
                25);
        }
        _absorptionRing = CreateEffect("Goal Absorption Ring", catalog.ballRing, 24);
        _goalBurst = CreateEffect("Goal Burst Ring", catalog.ballRing, 25);
        _goalEffects = new[]
        {
            CreateEffect("Goal Arc Top Left", catalog.goalArcTopLeft, 23),
            CreateEffect("Goal Arc Top Right", catalog.goalArcTopRight, 23),
            CreateEffect("Goal Arc Bottom Left", catalog.goalArcBottomLeft, 23),
            CreateEffect("Goal Arc Bottom Right", catalog.goalArcBottomRight, 23),
            CreateEffect("Goal Spark", catalog.goalSpark, 24)
        };
    }

    private ArcaneSpriteEffect CreateEffect(string effectName, Sprite[] sprites, int sortingOrder)
    {
        var child = new GameObject(effectName);
        child.transform.SetParent(transform, false);
        var effect = child.AddComponent<ArcaneSpriteEffect>();
        effect.Initialize(sprites, _effectMaterial, sortingOrder);
        return effect;
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
        _runeBurst?.Play(
            runePosition,
            0.42f,
            Vector3.one * 0.42f,
            Vector3.one * 0.78f,
            new Color(0.25f, 0.9f, 1f, 1f));
        _absorptionRing?.Play(
            ballPosition,
            runePosition,
            0.22f,
            Vector3.one * 1.15f,
            Vector3.one * 0.18f,
            new Color(0.55f, 0.12f, 1f, 1f));
        _goalBurst?.Play(
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
