using UnityEngine;

[CreateAssetMenu(menuName = "Pinball/Arcane VFX Catalog")]
public sealed class ArcaneVfxCatalog : ScriptableObject
{
    private const string ResourcePath = "ArcaneVFX/ArcaneVfxCatalog";

    [Header("Masks")]
    public Sprite ballMask;
    public Sprite standardBumperMask;
    public Sprite specialBumperMask;
    public Sprite magnetMask;
    public Sprite reflectorMask;
    public Sprite guardianRuneMask;
    public Sprite rangerRuneMask;
    public Sprite mageRuneMask;
    public Sprite lancerRuneMask;

    [Header("Ball")]
    public Sprite[] ballTrail;
    public Sprite[] ballImpact;
    public Sprite[] ballRing;

    [Header("Magnet")]
    public Sprite[] magnetArc;
    public Sprite[] magnetSpark;

    [Header("Goal")]
    public Sprite[] goalRing;
    public Sprite[] goalArcTopLeft;
    public Sprite[] goalArcTopRight;
    public Sprite[] goalArcBottomLeft;
    public Sprite[] goalArcBottomRight;
    public Sprite[] goalSpark;

    [Header("Reward")]
    public Sprite goldIcon;

    private static ArcaneVfxCatalog cached;

    public static ArcaneVfxCatalog Load()
    {
        if (cached == null) cached = Resources.Load<ArcaneVfxCatalog>(ResourcePath);
        return cached;
    }

    public Sprite GetMaskFor(string objectName)
    {
        if (objectName.Contains("StandardBumper")) return standardBumperMask;
        if (objectName.Contains("SpecialBumper")) return specialBumperMask;
        if (objectName.Contains("Magnet")) return magnetMask;
        if (objectName.Contains("Reflector")) return reflectorMask;
        return null;
    }

    public Sprite GetRuneMask(string unitId)
    {
        return unitId switch
        {
            "archer" => rangerRuneMask,
            "mage" => mageRuneMask,
            "lancer" => lancerRuneMask,
            _ => guardianRuneMask
        };
    }
}
