#if UNITY_EDITOR
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.SceneManagement;

using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ArcanePinballBoardSetup
{
    private const string ScenePath = "Assets/01. Scenes/02. Game.unity";
    private const string ArtRoot = "Assets/03. Images/Pinball/Arcane/";
    private const string MaterialPath = "Assets/09. Materials/Pinball/ArcaneDeviceAdditive.mat";
    private const string LayoutMarkerName = "ArcaneBoardLayoutV2";

    static ArcanePinballBoardSetup()
    {
        EditorApplication.delayCall += ApplyOnceInInteractiveEditor;
    }

    private static void ApplyOnceInInteractiveEditor()
    {
        if (Application.isBatchMode || EditorApplication.isCompiling) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) return;
        var absoluteScenePath = System.IO.Path.GetFullPath(ScenePath);
        if (System.IO.File.ReadAllText(absoluteScenePath).Contains($"m_Name: {LayoutMarkerName}")) return;
        Apply();
    }

    [MenuItem("Tools/Pinball/Apply Arcane Board")]
    public static void Apply()
    {
        ConfigureSpriteImports();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var manager = Object.FindFirstObjectByType<PinballManager>();
        if (manager == null)
        {
            throw new MissingComponentException("PinballManager was not found in 02. Game.");
        }

        DisableTemporaryBoard(manager.transform);
        UpdatePooledBalls(manager);

        var previous = manager.transform.Find("ArcaneBoard");
        if (previous != null)
        {
            Object.DestroyImmediate(previous.gameObject);
        }

        var additiveMaterial = GetOrCreateAdditiveMaterial();
        var board = CreateChild(manager.transform, "ArcaneBoard", new Vector3(6.4f, -0.65f, 0f));
        CreateChild(board.transform, LayoutMarkerName, Vector3.zero);
        CreateBoardVisual(board.transform);

        var wallMaterial = GetOrCreatePhysicsMaterial();
        CreateStaticColliders(board.transform, wallMaterial);
        CreateObstacles(board.transform, additiveMaterial, wallMaterial);
        CreateGoals(board.transform, additiveMaterial);
        var launcher = CreateLauncher(board.transform);

        SetObjectReference(manager, "launcherController", launcher);
        SetVector2(manager, "launchPosition", launcher.LoadPosition);
        SetFloat(manager, "minimumLaunchSpeed", 5.5f);
        SetFloat(manager, "maximumLaunchSpeed", 8f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[ArcanePinballBoardSetup] Applied arcane board to 02. Game.");
    }

    private static void ConfigureSpriteImports()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot.TrimEnd('/') });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = path.Contains("ball_arcane") ? 950f :
                path.Contains("pinball_board") ? 100f : 400f;
            importer.SaveAndReimport();
        }
    }

    private static void DisableTemporaryBoard(Transform managerRoot)
    {
        var preservedNames = new HashSet<string> { "Balls", "ArcaneBoard" };
        for (var index = 0; index < managerRoot.childCount; index++)
        {
            var child = managerRoot.GetChild(index);
            if (!preservedNames.Contains(child.name))
            {
                child.gameObject.SetActive(false);
            }
        }

        var oldBoard = GameObject.Find("pinball_board3_0");
        if (oldBoard != null) oldBoard.SetActive(false);
    }

    private static void UpdatePooledBalls(PinballManager manager)
    {
        var ballSprite = LoadSprite("ball_arcane.png");
        foreach (var ball in manager.GetComponentsInChildren<Pinball>(true))
        {
            var renderer = ball.GetComponent<SpriteRenderer>();
            renderer.sprite = ballSprite;
            renderer.sortingOrder = 110;

            var collider = ball.GetComponent<CircleCollider2D>();
            collider.radius = 0.3f;

            var body = ball.GetComponent<Rigidbody2D>();
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.gravityScale = 1f;
        }
    }

    private static void CreateBoardVisual(Transform parent)
    {
        var visual = CreateSprite(
            parent,
            "BoardVisual",
            "pinball_board_arcane.png",
            Vector2.zero,
            0,
            0.65f);
        visual.transform.localPosition = new Vector3(0f, 0f, 0.5f);
    }

    private static void CreateStaticColliders(Transform parent, PhysicsMaterial2D material)
    {
        var colliders = CreateChild(parent, "StaticColliders", Vector3.zero).transform;

        CreateBox(colliders, "LeftWall", new Vector2(-3.08f, 0f), new Vector2(0.18f, 9.8f), material);
        CreateBox(colliders, "LaunchOuterWall", new Vector2(3.08f, 0f), new Vector2(0.18f, 9.8f), material);
        CreateBox(colliders, "LaunchInnerWall", new Vector2(2.42f, -0.25f), new Vector2(0.14f, 7.1f), material);

        CreateEdge(colliders, "TopRailOuter", new[]
        {
            new Vector2(3.08f, 3.55f), new Vector2(2.95f, 4.15f),
            new Vector2(2.45f, 4.55f), new Vector2(1.6f, 4.78f),
            new Vector2(0.5f, 4.9f), new Vector2(-0.7f, 4.86f),
            new Vector2(-1.75f, 4.62f), new Vector2(-2.5f, 4.18f),
            new Vector2(-2.88f, 3.55f)
        }, material);
        CreateEdge(colliders, "TopRailInner", new[]
        {
            new Vector2(2.42f, 3.4f), new Vector2(2.2f, 3.82f),
            new Vector2(1.5f, 4.05f), new Vector2(0.5f, 4.16f),
            new Vector2(-0.55f, 4.12f), new Vector2(-1.42f, 3.92f),
            new Vector2(-2.05f, 3.55f), new Vector2(-2.28f, 3.15f)
        }, material);

        CreateEdge(colliders, "LeftGoalGuide", new[]
        {
            new Vector2(-3f, -2.3f), new Vector2(-2.75f, -2.85f),
            new Vector2(-2.42f, -3.35f)
        }, material);
        CreateEdge(colliders, "RightGoalGuide", new[]
        {
            new Vector2(2.25f, -2.3f), new Vector2(2.35f, -2.85f),
            new Vector2(2.42f, -3.35f)
        }, material);

        foreach (var x in new[] { -2.4f, -1.2f, 0f, 1.2f, 2.4f })
        {
            CreateBox(colliders, $"GoalDivider_{x:0.0}", new Vector2(x, -4.18f), new Vector2(0.1f, 1.15f), material);
        }
    }

    private static void CreateObstacles(
        Transform parent,
        Material additiveMaterial,
        PhysicsMaterial2D physicsMaterial)
    {
        var obstacles = CreateChild(parent, "Obstacles", Vector3.zero).transform;

        CreateTriangleBumper(obstacles, "StandardBumper_Top", new Vector2(0f, 2.7f), physicsMaterial);
        CreateTriangleBumper(obstacles, "StandardBumper_Left", new Vector2(-1.25f, 1.55f), physicsMaterial);
        CreateTriangleBumper(obstacles, "StandardBumper_Right", new Vector2(1.25f, 1.55f), physicsMaterial);
        CreateRoundBumper(obstacles, "SpecialBumper", new Vector2(0f, 0.15f), additiveMaterial, physicsMaterial);

        var pinPositions = new[]
        {
            new Vector2(-1.85f, 0.35f), new Vector2(1.85f, 0.35f),
            new Vector2(-1.25f, -0.35f), new Vector2(1.25f, -0.35f),
            new Vector2(-0.7f, -1.0f), new Vector2(0.7f, -1.0f),
            new Vector2(-0.35f, -1.65f), new Vector2(0.35f, -1.65f)
        };
        for (var index = 0; index < pinPositions.Length; index++)
        {
            CreateSmallPin(obstacles, $"SmallPin_{index + 1}", pinPositions[index], physicsMaterial);
        }

        CreateMagnet(obstacles, "Magnet_Left", new Vector2(-1.55f, -1.85f), false, additiveMaterial);
        CreateMagnet(obstacles, "Magnet_Right", new Vector2(1.55f, -1.85f), true, additiveMaterial);
        CreateReflector(obstacles, "Reflector_Left", new Vector2(-1.45f, -2.85f), false, physicsMaterial);
        CreateReflector(obstacles, "Reflector_Right", new Vector2(1.45f, -2.85f), true, physicsMaterial);
    }

    private static void CreateTriangleBumper(
        Transform parent,
        string name,
        Vector2 position,
        PhysicsMaterial2D material)
    {
        var renderer = CreateSprite(parent, name, "bumper_standard.png", position, 10, 0.72f);
        var collider = renderer.gameObject.AddComponent<PolygonCollider2D>();
        collider.points = new[]
        {
            new Vector2(0f, 0.62f), new Vector2(-0.62f, -0.48f), new Vector2(0.62f, -0.48f)
        };
        collider.sharedMaterial = material;
        AddObstacle(renderer.gameObject, EPinballObstacle.BigBumper);
    }

    private static void CreateRoundBumper(
        Transform parent,
        string name,
        Vector2 position,
        Material additiveMaterial,
        PhysicsMaterial2D material)
    {
        var renderer = CreateSprite(parent, name, "bumper_special.png", position, 10, 0.58f);
        var collider = renderer.gameObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.72f;
        collider.sharedMaterial = material;
        AddObstacle(renderer.gameObject, EPinballObstacle.BigBumper);
        AddMask(renderer.transform, "bumper_special_mask.png", additiveMaterial);
    }

    private static void CreateSmallPin(
        Transform parent,
        string name,
        Vector2 position,
        PhysicsMaterial2D material)
    {
        var renderer = CreateSprite(parent, name, "pin_small.png", position, 10, 0.36f);
        var collider = renderer.gameObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.25f;
        collider.sharedMaterial = material;
        AddObstacle(renderer.gameObject, EPinballObstacle.SmallPin);
    }

    private static void CreateMagnet(
        Transform parent,
        string name,
        Vector2 position,
        bool flipX,
        Material additiveMaterial)
    {
        var renderer = CreateSprite(parent, name, "magnet_device.png", position, 10, 0.52f);
        renderer.flipX = flipX;
        var collider = renderer.gameObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.72f;
        collider.isTrigger = true;
        var controller = renderer.gameObject.AddComponent<PinballMagnetController>();
        SetObjectReference(controller, "pinballManager", Object.FindFirstObjectByType<PinballManager>());
        SetObjectReference(controller, "targetRenderer", renderer);
        AddMask(renderer.transform, "magnet_device_mask.png", additiveMaterial, flipX);
    }

    private static void CreateReflector(
        Transform parent,
        string name,
        Vector2 position,
        bool flipX,
        PhysicsMaterial2D material)
    {
        var renderer = CreateSprite(parent, name, "reflector_auto.png", position, 10, 0.68f);
        renderer.flipX = flipX;
        var collider = renderer.gameObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1.2f, 0.36f);
        collider.sharedMaterial = material;
        var controller = renderer.gameObject.AddComponent<PinballReflectorController>();
        SetVector2(controller, "outwardNormal", flipX ? new Vector2(-0.35f, 1f) : new Vector2(0.35f, 1f));
    }

    private static void CreateGoals(Transform parent, Material additiveMaterial)
    {
        var goals = CreateChild(parent, "Goals", Vector3.zero).transform;
        var unitIds = new[] { "warrior", "archer", "mage", "lancer" };
        var sprites = new[] { "rune_guardian.png", "rune_ranger.png", "rune_mage.png", "rune_lancer.png" };
        var masks = new[] { "rune_guardian_mask.png", "rune_ranger_mask.png", "rune_mage_mask.png", "rune_lancer_mask.png" };

        for (var index = 0; index < 4; index++)
        {
            var x = -1.8f + index * 1.2f;
            var goalObject = CreateChild(goals, $"Goal_{index + 1}_{unitIds[index]}", new Vector3(x, -4.25f, 0f));
            var collider = goalObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.05f, 0.75f);
            collider.isTrigger = true;
            var goal = goalObject.AddComponent<PinballGoal>();
            SetGoalUnitData(goal, unitIds[index]);

            var renderer = CreateSprite(goalObject.transform, "Rune", sprites[index], new Vector2(0f, 0.28f), 12, 0.38f);
            AddMask(renderer.transform, masks[index], additiveMaterial);
        }

        CreateOutZone(goals, "OutZone_Left", new Vector2(-2.78f, -4.4f));
        CreateOutZone(goals, "OutZone_Right", new Vector2(2.78f, -4.4f));
    }

    private static PinballLauncherController CreateLauncher(Transform parent)
    {
        var launcher = CreateChild(parent, "Launcher", Vector3.zero).transform;
        var loadPoint = CreateChild(launcher, "LoadPoint", new Vector3(2.75f, -3.72f, 0f)).transform;
        CreateSprite(launcher, "PlungerBase", "plunger_base.png", new Vector2(2.75f, -4.45f), 14, 0.8f);
        var piston = CreateSprite(launcher, "PlungerPiston", "plunger_piston.png", new Vector2(2.75f, -3.78f), 14, 0.72f).transform;
        var spring = CreateSprite(launcher, "PlungerSpring", "plunger_spring.png", new Vector2(2.75f, -3.55f), 13, 0.5f).transform;
        var lever = CreateSprite(launcher, "PlungerLever", "plunger_lever.png", new Vector2(3.05f, -4.12f), 16, 0.75f);
        var interaction = lever.gameObject.AddComponent<CircleCollider2D>();
        interaction.radius = 0.38f;
        interaction.isTrigger = true;
        var controller = lever.gameObject.AddComponent<PinballLauncherController>();

        SetObjectReference(controller, "pinballManager", Object.FindFirstObjectByType<PinballManager>());
        SetObjectReference(controller, "loadPoint", loadPoint);
        SetObjectReference(controller, "piston", piston);
        SetObjectReference(controller, "spring", spring);
        SetVector2(controller, "launchDirection", Vector2.up);
        return controller;
    }

    private static void CreateOutZone(Transform parent, string name, Vector2 position)
    {
        var gameObject = CreateChild(parent, name, new Vector3(position.x, position.y, 0f));
        var collider = gameObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.55f, 0.95f);
        collider.isTrigger = true;
        gameObject.AddComponent<PinballOutZone>();
    }

    private static SpriteRenderer CreateSprite(
        Transform parent,
        string name,
        string spriteName,
        Vector2 localPosition,
        int sortingOrder,
        float scale = 1f)
    {
        var gameObject = CreateChild(parent, name, new Vector3(localPosition.x, localPosition.y, 0f));
        gameObject.transform.localScale = Vector3.one * scale;
        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(spriteName);
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static void AddMask(Transform parent, string spriteName, Material material, bool flipX = false)
    {
        // The generated grayscale files are shader masks, not visible overlay sprites.
        // Rendering them directly with an additive material creates large white blobs.
    }

    private static void AddObstacle(GameObject gameObject, EPinballObstacle type)
    {
        var obstacle = gameObject.AddComponent<PinballObstacle>();
        var serialized = new SerializedObject(obstacle);
        serialized.FindProperty("type").enumValueIndex = (int)type;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static BoxCollider2D CreateBox(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        PhysicsMaterial2D material)
    {
        var gameObject = CreateChild(parent, name, new Vector3(position.x, position.y, 0f));
        var collider = gameObject.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.sharedMaterial = material;
        return collider;
    }

    private static void CreateEdge(
        Transform parent,
        string name,
        Vector2[] points,
        PhysicsMaterial2D material)
    {
        var gameObject = CreateChild(parent, name, Vector3.zero);
        var collider = gameObject.AddComponent<EdgeCollider2D>();
        collider.points = points;
        collider.edgeRadius = 0.06f;
        collider.sharedMaterial = material;
    }

    private static GameObject CreateChild(Transform parent, string name, Vector3 localPosition)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = localPosition;
        return gameObject;
    }

    private static Sprite LoadSprite(string fileName)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + fileName);
        if (sprite == null) throw new MissingReferenceException($"Sprite not found: {fileName}");
        return sprite;
    }

    private static Material GetOrCreateAdditiveMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material != null) return material;

        EnsureFolder("Assets/09. Materials/Pinball");
        var shader = Shader.Find("Pinball/ArcaneAdditive");
        if (shader == null) throw new MissingReferenceException("Pinball/ArcaneAdditive shader was not found.");
        material = new Material(shader) { name = "ArcaneDeviceAdditive" };
        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }

    private static PhysicsMaterial2D GetOrCreatePhysicsMaterial()
    {
        const string path = "Assets/09. Materials/Pinball/ArcaneBoardPhysics.physicsMaterial2D";
        var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (material != null) return material;

        EnsureFolder("Assets/09. Materials/Pinball");
        material = new PhysicsMaterial2D("ArcaneBoardPhysics")
        {
            friction = 0.02f,
            bounciness = 0.78f
        };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        var segments = path.Split('/');
        var current = segments[0];
        for (var index = 1; index < segments.Length; index++)
        {
            var next = current + "/" + segments[index];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, segments[index]);
            }
            current = next;
        }
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetVector2(Object target, string propertyName, Vector2 value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).vector2Value = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string propertyName, float value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetGoalUnitData(PinballGoal goal, string unitId)
    {
        var serialized = new SerializedObject(goal);
        var unitData = serialized.FindProperty("unitData");
        unitData.FindPropertyRelative("UnitId").stringValue = unitId;
        unitData.FindPropertyRelative("Level").intValue = 1;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
