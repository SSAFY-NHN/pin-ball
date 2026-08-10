using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PinballOutZone : MonoBehaviour
{
    private PinballManager _pinballManager;
    private ArcaneMaskGlowController _glow;
    private ArcaneSpriteEffect _ringEffect;
    private ArcaneSpriteEffect _impactEffect;
    private Material _effectMaterial;

    private void Awake()
    {
        _glow = GetComponent<ArcaneMaskGlowController>();

        var catalog = ArcaneVfxCatalog.Load();
        var shader = Resources.Load<Shader>("ArcaneVFX/ArcaneAdditive");
        if (catalog == null || shader == null) return;

        _effectMaterial = new Material(shader)
        {
            name = "Arcane Out Zone VFX (Runtime)",
            hideFlags = HideFlags.HideAndDontSave
        };
        _effectMaterial.SetFloat("_Intensity", 1.75f);
        _effectMaterial.SetFloat("_GlowSpread", 1.2f);
        _ringEffect = CreateEffect("Out Zone Ring", catalog.ballRing, 24);
        _impactEffect = CreateEffect("Out Zone Impact", catalog.ballImpact, 25);
    }

    private void Start()
    {
        _pinballManager = App.Get<PinballManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var ball = other.GetComponent<Pinball>();
        if (ball == null) return;

        PlayMissEffect(ball.transform.position);
        _pinballManager.OnMissedBall(ball);
    }

    private void PlayMissEffect(Vector2 ballPosition)
    {
        var position = new Vector3(ballPosition.x, ballPosition.y, transform.position.z);
        var failureColor = new Color(1f, 0.12f, 0.2f, 0.9f);
        _glow?.Pulse(2.2f, 0.28f);
        _ringEffect?.Play(position, 0.3f,
            Vector3.one * 0.55f,
            Vector3.one * 1.15f,
            failureColor);
        _impactEffect?.Play(position, 0.2f,
            Vector3.one * 0.5f,
            Vector3.one * 0.85f,
            failureColor);
    }

    private ArcaneSpriteEffect CreateEffect(
        string effectName,
        Sprite[] sprites,
        int sortingOrder)
    {
        var child = new GameObject(effectName);
        child.transform.SetParent(transform, false);
        var effect = child.AddComponent<ArcaneSpriteEffect>();
        effect.Initialize(sprites, _effectMaterial, sortingOrder);
        return effect;
    }

    private void OnDestroy()
    {
        if (_effectMaterial != null) Destroy(_effectMaterial);
    }
}
