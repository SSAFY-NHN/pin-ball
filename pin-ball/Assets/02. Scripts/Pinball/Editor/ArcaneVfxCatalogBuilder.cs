#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ArcaneVfxCatalogBuilder
{
    private const string ArtRoot = "Assets/03. Images/Pinball/Arcane/";
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
        SetSprite(serialized, "ballMask", "ball_arcane_mask.png");
        SetSprite(serialized, "standardBumperMask", "bumper_standard_mask.png");
        SetSprite(serialized, "specialBumperMask", "bumper_special_mask.png");
        SetSprite(serialized, "magnetMask", "magnet_device_mask.png");
        SetSprite(serialized, "reflectorMask", "reflector_auto_mask.png");
        SetSprite(serialized, "guardianRuneMask", "rune_guardian_mask.png");
        SetSprite(serialized, "rangerRuneMask", "rune_ranger_mask.png");
        SetSprite(serialized, "mageRuneMask", "rune_mage_mask.png");
        SetSprite(serialized, "lancerRuneMask", "rune_lancer_mask.png");
        SetSprites(serialized, "ballTrail", "vfx_ball_trail.png");
        SetSprites(serialized, "ballImpact", "vfx_ball_impact.png");
        SetSprites(serialized, "ballRing", "vfx_ball_ring.png");
        SetSprites(serialized, "magnetArc", "vfx_magnet_arc.png");
        SetSprites(serialized, "magnetSpark", "vfx_magnet_spark.png");
        SetSprites(serialized, "goalArcTopLeft", "vfx_goal_arc_top_left.png");
        SetSprites(serialized, "goalArcTopRight", "vfx_goal_arc_top_right.png");
        SetSprites(serialized, "goalArcBottomLeft", "vfx_goal_arc_bottom_left.png");
        SetSprites(serialized, "goalArcBottomRight", "vfx_goal_arc_bottom_right.png");
        SetSprites(serialized, "goalSpark", "vfx_goal_spark.png");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }

    private static void SetSprite(SerializedObject serialized, string field, string file)
    {
        serialized.FindProperty(field).objectReferenceValue = LoadSprites(file).FirstOrDefault();
    }

    private static void SetSprites(SerializedObject serialized, string field, string file)
    {
        var sprites = LoadSprites(file);
        var property = serialized.FindProperty(field);
        property.arraySize = sprites.Length;
        for (var index = 0; index < sprites.Length; index++)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue = sprites[index];
        }
    }

    private static Sprite[] LoadSprites(string file)
    {
        return AssetDatabase.LoadAllAssetsAtPath(ArtRoot + file)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name)
            .ToArray();
    }
}
#endif
