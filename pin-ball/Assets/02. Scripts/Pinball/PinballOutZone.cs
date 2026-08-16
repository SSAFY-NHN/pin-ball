using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PinballOutZone : MonoBehaviour
{
    [SerializeField] private Material effectMaterial;
    [SerializeField] private ArcaneSpriteEffect ringEffect;
    [SerializeField] private ArcaneSpriteEffect impactEffect;

    private PinballManager _pinballManager;
    private ArcaneMaskGlowController _glow;

    private void Awake()
    {
        _glow = GetComponent<ArcaneMaskGlowController>();

        var catalog = ArcaneVfxCatalog.Load();
        if (catalog == null) return;

        ringEffect?.Initialize(catalog.ballRing, effectMaterial, 24);
        impactEffect?.Initialize(catalog.ballImpact, effectMaterial, 25);
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
        ringEffect?.Play(position, 0.3f,
            Vector3.one * 0.55f,
            Vector3.one * 1.15f,
            failureColor);
        impactEffect?.Play(position, 0.2f,
            Vector3.one * 0.5f,
            Vector3.one * 0.85f,
            failureColor);
    }

}
