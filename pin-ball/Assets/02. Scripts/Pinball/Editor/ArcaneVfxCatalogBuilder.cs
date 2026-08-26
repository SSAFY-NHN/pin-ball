#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ArcaneVfxCatalogBuilder
{
    private const string ArcaneArtRoot = "Assets/03. Images/Pinball/Arcane/";
    private const string MoonlitArtRoot =
        "Assets/03. Images/Pinball/MoonlitWorkshop/";
    private const string CatalogPath = "Assets/Resources/ArcaneVFX/ArcaneVfxCatalog.asset";

    static ArcaneVfxCatalogBuilder()
    {
        EditorApplication.delayCall += EnsureCatalog;
    }

    [MenuItem("Tools/Pinball/Refresh Arcane VFX Catalog")]
    public static void EnsureCatalog()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<ArcaneVfxCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<ArcaneVfxCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        var serialized = new SerializedObject(catalog);
        SetSprite(serialized, "ballMask", ArcaneArtRoot, "ball_arcane_mask.png");
        SetSprite(
            serialized,
            "standardBumperMask",
            MoonlitArtRoot,
            "bumper_standard_mask.png");
        SetSprite(
            serialized,
            "specialBumperMask",
            MoonlitArtRoot,
            "bumper_jackpot_mask.png");
        SetSprite(
            serialized,
            "magnetMask",
            MoonlitArtRoot,
            "Obstacles/obstacle_clockwork_spinner_mask.png");
        SetSprite(
            serialized,
            "reflectorMask",
            MoonlitArtRoot,
            "deflector_bar_mask.png");
        SetSprite(
            serialized,
            "guardianRuneMask",
            MoonlitArtRoot,
            "Obstacles/bumper_clockwork_gear_mask.png");
        SetSprite(
            serialized,
            "rangerRuneMask",
            MoonlitArtRoot,
            "Obstacles/obstacle_clockwork_spinner_mask.png");
        SetSprite(
            serialized,
            "mageRuneMask",
            MoonlitArtRoot,
            "Obstacles/obstacle_forge_cross_mask.png");
        SetSprite(
            serialized,
            "lancerRuneMask",
            MoonlitArtRoot,
            "Obstacles/obstacle_spring_gate_mask.png");
        SetSprites(serialized, "ballTrail", ArcaneArtRoot, "vfx_ball_trail.png");
        SetSprites(serialized, "ballImpact", ArcaneArtRoot, "vfx_ball_impact.png");
        SetSprites(serialized, "ballRing", ArcaneArtRoot, "vfx_ball_ring.png");
        SetSprites(
            serialized,
            "magnetArc",
            MoonlitArtRoot,
            "Obstacles/obstacle_clockwork_spinner_mask.png");
        SetSprites(
            serialized,
            "magnetSpark",
            MoonlitArtRoot,
            "pin_small_mask.png");
        SetSprites(
            serialized,
            "goalRing",
            MoonlitArtRoot,
            "bumper_jackpot_mask.png");
        SetSprites(
            serialized,
            "goalArcTopLeft",
            MoonlitArtRoot,
            "guide_rail_mask.png");
        SetSprites(
            serialized,
            "goalArcTopRight",
            MoonlitArtRoot,
            "guide_rail_mask.png");
        SetSprites(
            serialized,
            "goalArcBottomLeft",
            MoonlitArtRoot,
            "guide_rail_mask.png");
        SetSprites(
            serialized,
            "goalArcBottomRight",
            MoonlitArtRoot,
            "guide_rail_mask.png");
        SetSprites(
            serialized,
            "goalSpark",
            MoonlitArtRoot,
            "pin_small_mask.png");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }

    private static void SetSprite(
        SerializedObject serialized,
        string field,
        string root,
        string file)
    {
        serialized.FindProperty(field).objectReferenceValue =
            LoadSprites(root, file).FirstOrDefault();
    }

    private static void SetSprites(
        SerializedObject serialized,
        string field,
        string root,
        string file)
    {
        var sprites = LoadSprites(root, file);
        var property = serialized.FindProperty(field);
        property.arraySize = sprites.Length;
        for (var index = 0; index < sprites.Length; index++)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue = sprites[index];
        }
    }

    private static Sprite[] LoadSprites(string root, string file)
    {
        return AssetDatabase.LoadAllAssetsAtPath(root + file)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name)
            .ToArray();
    }
}
#endif
