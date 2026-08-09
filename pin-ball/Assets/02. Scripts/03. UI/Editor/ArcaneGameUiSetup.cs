#if UNITY_EDITOR
using System;

using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ArcaneGameUiSetup
{
    private const string GameScenePath =
        "Assets/01. Scenes/02. Game.unity";
    private const string TopHudName = "ArcaneTopHud";
    private const string BottomPanelName = "ArcaneBottomPanel";
    private const int WaveNodeCount = 10;
    private const int WaveConnectorCount = WaveNodeCount - 1;
    private const string HudAssetPath =
        "Assets/03. Images/UI/ArcaneHud/";
    private const string UiAssetPath =
        "Assets/03. Images/UI/";

    public static void Apply()
    {
        try
        {
            Scene scene = EditorSceneManager.OpenScene(GameScenePath);
            var statusPanel = FindRequired<StatusPanel>();
            var wavePanel = FindRequired<WavePanel>();
            var bottomTabPanel = FindOrCreateBottomTabPanel();

            ConfigureSpriteImports(HudAssetPath);
            ConfigureSpriteImports(UiAssetPath, false);

            RestoreFunctionalObjects(
                statusPanel,
                wavePanel,
                bottomTabPanel);
            DestroyGeneratedRoot(TopHudName);
            DestroyGeneratedRoot(BottomPanelName);

            BuildTopHud(statusPanel, wavePanel);
            BuildBottomPanel(bottomTabPanel);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Arcane UI application completed");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    public static void Validate()
    {
        try
        {
            EditorSceneManager.OpenScene(GameScenePath);
            var statusPanel = FindRequired<StatusPanel>();
            var wavePanel = FindRequired<WavePanel>();
            var bottomTabPanel = FindRequired<BottomTabPanel>();

            ValidateTopHud(statusPanel, wavePanel);
            ValidateBottomPanel(bottomTabPanel);
            ValidateSpriteImports(HudAssetPath);
            ValidateSpriteImports(UiAssetPath, false);
            Debug.Log("Arcane UI validation passed");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void BuildTopHud(
        StatusPanel statusPanel,
        WavePanel wavePanel)
    {
        RectTransform statusTransform =
            (RectTransform)statusPanel.transform;
        RectTransform root = CreateRectTransform(
            TopHudName,
            statusTransform);
        Stretch(root);
        root.SetAsFirstSibling();

        Image frame = CreateImage("Frame", root);
        Stretch((RectTransform)frame.transform);
        frame.sprite = LoadHudSprite("ui_hud_top_composite.png");
        frame.preserveAspect = false;
        frame.raycastTarget = false;

        var statusSerialized = new SerializedObject(statusPanel);
        var hpText = (TextMeshProUGUI)statusSerialized
            .FindProperty("playerHpText").objectReferenceValue;
        var goldText = (TextMeshProUGUI)statusSerialized
            .FindProperty("goldText").objectReferenceValue;
        var oldWaveText = (TextMeshProUGUI)statusSerialized
            .FindProperty("waveText").objectReferenceValue;
        Require(hpText != null, "StatusPanel.playerHpText");
        Require(goldText != null, "StatusPanel.goldText");

        ReparentAndPlace(hpText.rectTransform, root, 118f, 0f, 92f, 52f);
        ReparentAndPlace(goldText.rectTransform, root, 285f, 0f, 118f, 52f);
        ConfigureHudText(hpText, 30f, TextAlignmentOptions.Left);
        ConfigureHudText(goldText, 30f, TextAlignmentOptions.Left);
        if (oldWaveText != null)
        {
            oldWaveText.gameObject.SetActive(false);
        }

        Image hpIcon = CreateImage("HpIcon", root);
        Place((RectTransform)hpIcon.transform, 65f, 0f, 58f, 58f);
        hpIcon.sprite = LoadHudSprite("ui_icon_hp.png");
        hpIcon.preserveAspect = true;
        hpIcon.raycastTarget = false;

        Image goldIcon = CreateImage("GoldIcon", root);
        Place((RectTransform)goldIcon.transform, 235f, 0f, 54f, 54f);
        goldIcon.sprite = LoadHudSprite("ui_icon_gold.png");
        goldIcon.preserveAspect = true;
        goldIcon.raycastTarget = false;

        Sprite idleNode = LoadHudSprite("ui_wave_node_idle.png");
        Sprite lockedNode = LoadHudSprite("ui_wave_node_locked.png");
        Sprite currentNode = LoadHudSprite("ui_wave_node_current.png");
        Sprite completeNode = LoadHudSprite("ui_wave_node_complete.png");
        Sprite elite05Node = LoadHudSprite("ui_wave_node_elite_05.png");
        Sprite elite09Node = LoadHudSprite("ui_wave_node_elite_09.png");
        Sprite boss10Node = LoadHudSprite("ui_wave_node_boss_10.png");
        Sprite idleConnector = LoadHudSprite("ui_wave_connector_idle.png");
        Sprite completeConnector = LoadHudSprite(
            "ui_wave_connector_complete.png");

        var nodes = new Image[WaveNodeCount];
        var connectors = new Image[WaveConnectorCount];
        var numberTexts = new TextMeshProUGUI[WaveNodeCount];
        const float startX = 430f;
        const float spacing = 112f;

        for (int index = 0; index < WaveConnectorCount; index++)
        {
            Image connector = CreateImage(
                $"WaveConnector_{index + 1:00}",
                root);
            Place(
                (RectTransform)connector.transform,
                startX + spacing * index + spacing * 0.5f,
                0f,
                72f,
                18f);
            connector.sprite = idleConnector;
            connector.preserveAspect = false;
            connector.raycastTarget = false;
            connectors[index] = connector;
        }

        for (int index = 0; index < WaveNodeCount; index++)
        {
            Image node = CreateImage($"WaveNode_{index + 1:00}", root);
            Place(
                (RectTransform)node.transform,
                startX + spacing * index,
                0f,
                76f,
                76f);
            node.sprite = idleNode;
            node.preserveAspect = true;
            node.raycastTarget = false;
            node.transform.SetAsLastSibling();

            TextMeshProUGUI numberText = CreateText(
                "Number",
                (RectTransform)node.transform,
                hpText.font);
            Stretch(numberText.rectTransform);
            ConfigureHudText(
                numberText,
                25f,
                TextAlignmentOptions.Center);
            numberText.text = (index + 1).ToString();
            numberText.raycastTarget = false;
            nodes[index] = node;
            numberTexts[index] = numberText;
        }

        AssignObjectArray(statusSerialized, "waveNodes", nodes);
        AssignObjectArray(statusSerialized, "waveConnectors", connectors);
        AssignObjectArray(
            statusSerialized,
            "waveNumberTexts",
            numberTexts);
        AssignObject(statusSerialized, "idleNodeSprite", idleNode);
        AssignObject(statusSerialized, "lockedNodeSprite", lockedNode);
        AssignObject(statusSerialized, "currentNodeSprite", currentNode);
        AssignObject(statusSerialized, "completeNodeSprite", completeNode);
        AssignObject(statusSerialized, "elite05NodeSprite", elite05Node);
        AssignObject(statusSerialized, "elite09NodeSprite", elite09Node);
        AssignObject(statusSerialized, "boss10NodeSprite", boss10Node);
        AssignObject(statusSerialized, "idleConnectorSprite", idleConnector);
        AssignObject(
            statusSerialized,
            "completeConnectorSprite",
            completeConnector);
        statusSerialized.ApplyModifiedPropertiesWithoutUndo();

        var waveSerialized = new SerializedObject(wavePanel);
        var startButton = (Button)waveSerialized
            .FindProperty("startButton").objectReferenceValue;
        var launchButton = (Button)waveSerialized
            .FindProperty("launchButton").objectReferenceValue;
        var launchCostText = (TextMeshProUGUI)waveSerialized
            .FindProperty("launchCostText").objectReferenceValue;
        Require(startButton != null, "WavePanel.startButton");
        Require(launchButton != null, "WavePanel.launchButton");
        Require(launchCostText != null, "WavePanel.launchCostText");

        ConfigureButton(
            startButton,
            root,
            1580f,
            0f,
            128f,
            86f,
            "ui_button_battle_state_normal.png",
            "ui_button_battle_state_pressed.png",
            "ui_button_battle_state_disabled.png");
        ConfigureButton(
            launchButton,
            root,
            1425f,
            0f,
            210f,
            78f,
            "ui_button_launch_normal.png",
            "ui_button_launch_pressed.png",
            "ui_button_launch_disabled.png");
        ReparentAndPlace(
            launchCostText.rectTransform,
            (RectTransform)launchButton.transform,
            0f,
            0f,
            180f,
            50f);
        ConfigureHudText(
            launchCostText,
            24f,
            TextAlignmentOptions.Center);

        Image settings = CreateImage("SettingsDecoration", root);
        Place(
            (RectTransform)settings.transform,
            1820f,
            0f,
            86f,
            86f);
        settings.sprite = LoadHudSprite("ui_button_settings_disabled.png");
        settings.preserveAspect = true;
        settings.raycastTarget = false;
    }

    private static void BuildBottomPanel(BottomTabPanel bottomTabPanel)
    {
        RectTransform parent = (RectTransform)bottomTabPanel.transform.parent;
        RectTransform root = CreateRectTransform(BottomPanelName, parent);
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(0.58f, 0.25f);
        root.offsetMin = new Vector2(12f, 12f);
        root.offsetMax = new Vector2(-8f, -8f);
        root.SetAsFirstSibling();

        Image frame = CreateImage("Frame", root);
        Stretch((RectTransform)frame.transform);
        frame.sprite = LoadUiSprite("ui_bottom_panel_frame.png");
        frame.preserveAspect = false;
        frame.raycastTarget = false;

        Image content = CreateImage("Content", root);
        Stretch((RectTransform)content.transform, 34f, 22f, 34f, 22f);
        content.sprite = LoadUiSprite("ui_bottom_panel_content.png");
        content.preserveAspect = false;
        content.raycastTarget = false;

        Image leftGem = CreateImage("GemLeft", root);
        PlaceAnchored(
            (RectTransform)leftGem.transform,
            new Vector2(0f, 0.5f),
            new Vector2(-6f, 0f),
            new Vector2(52f, 72f));
        leftGem.sprite = LoadUiSprite("ui_bottom_panel_gem_left.png");
        leftGem.preserveAspect = true;
        leftGem.raycastTarget = false;

        Image rightGem = CreateImage("GemRight", root);
        PlaceAnchored(
            (RectTransform)rightGem.transform,
            new Vector2(1f, 0.5f),
            new Vector2(6f, 0f),
            new Vector2(52f, 72f));
        rightGem.sprite = LoadUiSprite("ui_bottom_panel_gem_right.png");
        rightGem.preserveAspect = true;
        rightGem.raycastTarget = false;

        var serialized = new SerializedObject(bottomTabPanel);
        var itemsButton = (Button)serialized
            .FindProperty("itemsButton").objectReferenceValue;
        var shopButton = (Button)serialized
            .FindProperty("shopButton").objectReferenceValue;
        var itemsContent = (GameObject)serialized
            .FindProperty("itemsContent").objectReferenceValue;
        var shopContent = (GameObject)serialized
            .FindProperty("shopContent").objectReferenceValue;
        Require(itemsButton != null, "BottomTabPanel.itemsButton");
        Require(shopButton != null, "BottomTabPanel.shopButton");
        Require(itemsContent != null, "BottomTabPanel.itemsContent");
        Require(shopContent != null, "BottomTabPanel.shopContent");

        RectTransform contentRoot = CreateRectTransform("FunctionalContent", root);
        Stretch(contentRoot, 48f, 28f, 48f, 28f);
        itemsContent.transform.SetParent(contentRoot, false);
        shopContent.transform.SetParent(contentRoot, false);
        Stretch((RectTransform)itemsContent.transform);
        Stretch((RectTransform)shopContent.transform);

        itemsButton.transform.SetParent(root, false);
        shopButton.transform.SetParent(root, false);
        PlaceAnchored(
            (RectTransform)itemsButton.transform,
            new Vector2(0f, 1f),
            new Vector2(125f, -28f),
            new Vector2(150f, 48f));
        PlaceAnchored(
            (RectTransform)shopButton.transform,
            new Vector2(0f, 1f),
            new Vector2(285f, -28f),
            new Vector2(150f, 48f));
    }

    private static BottomTabPanel FindOrCreateBottomTabPanel()
    {
        var existing = UnityEngine.Object.FindFirstObjectByType<BottomTabPanel>(
            FindObjectsInactive.Include);
        if (existing != null)
        {
            return existing;
        }

        Canvas canvas = FindRequired<Canvas>();
        RectTransform controllerTransform = CreateRectTransform(
            "BottomTabController",
            canvas.transform);
        Stretch(controllerTransform);
        var controller = controllerTransform.gameObject
            .AddComponent<BottomTabPanel>();
        ItemPanel itemPanel = FindRequired<ItemPanel>();
        ShopPanel shopPanel = FindRequired<ShopPanel>();
        TextMeshProUGUI fontSource =
            UnityEngine.Object.FindFirstObjectByType<TextMeshProUGUI>(
                FindObjectsInactive.Include);
        Require(fontSource != null && fontSource.font != null, "TMP font source");

        Button itemsButton = CreateTabButton(
            "ItemsTabButton",
            controllerTransform,
            "아이템",
            fontSource.font);
        Button shopButton = CreateTabButton(
            "ShopTabButton",
            controllerTransform,
            "상점",
            fontSource.font);

        var serialized = new SerializedObject(controller);
        AssignObject(serialized, "itemsButton", itemsButton);
        AssignObject(serialized, "shopButton", shopButton);
        AssignObject(serialized, "itemsContent", itemPanel.gameObject);
        AssignObject(serialized, "shopContent", shopPanel.gameObject);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return controller;
    }

    private static Button CreateTabButton(
        string name,
        RectTransform parent,
        string label,
        TMP_FontAsset font)
    {
        Image image = CreateImage(name, parent);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        image.sprite = LoadHudSprite("ui_button_wave_start_normal.png");
        image.type = Image.Type.Simple;
        image.preserveAspect = false;

        Sprite normal = image.sprite;
        SpriteState state = button.spriteState;
        state.highlightedSprite = normal;
        state.selectedSprite = normal;
        state.pressedSprite = LoadHudSprite(
            "ui_button_wave_start_pressed.png");
        state.disabledSprite = LoadHudSprite(
            "ui_button_wave_start_disabled.png");
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = state;

        TextMeshProUGUI text = CreateText(
            "Label",
            (RectTransform)image.transform,
            font);
        Stretch(text.rectTransform);
        ConfigureHudText(text, 24f, TextAlignmentOptions.Center);
        text.text = label;
        return button;
    }

    private static void ValidateTopHud(
        StatusPanel statusPanel,
        WavePanel wavePanel)
    {
        Require(GameObject.Find(TopHudName) != null, TopHudName);
        var statusSerialized = new SerializedObject(statusPanel);
        ValidateObjectArray(
            statusSerialized,
            "waveNodes",
            WaveNodeCount);
        ValidateObjectArray(
            statusSerialized,
            "waveConnectors",
            WaveConnectorCount);
        ValidateObjectArray(
            statusSerialized,
            "waveNumberTexts",
            WaveNodeCount);

        string[] spriteFields =
        {
            "idleNodeSprite",
            "lockedNodeSprite",
            "currentNodeSprite",
            "completeNodeSprite",
            "elite05NodeSprite",
            "elite09NodeSprite",
            "boss10NodeSprite",
            "idleConnectorSprite",
            "completeConnectorSprite",
        };
        foreach (string field in spriteFields)
        {
            Require(
                statusSerialized.FindProperty(field).objectReferenceValue != null,
                $"StatusPanel.{field}");
        }

        var waveSerialized = new SerializedObject(wavePanel);
        Require(
            waveSerialized.FindProperty("startButton").objectReferenceValue != null,
            "WavePanel.startButton");
        Require(
            waveSerialized.FindProperty("launchButton").objectReferenceValue != null,
            "WavePanel.launchButton");
    }

    private static void ValidateBottomPanel(BottomTabPanel bottomTabPanel)
    {
        GameObject root = GameObject.Find(BottomPanelName);
        Require(root != null, BottomPanelName);
        string[] imageNames = { "Frame", "Content", "GemLeft", "GemRight" };
        foreach (string imageName in imageNames)
        {
            Transform child = root.transform.Find(imageName);
            Require(child != null, $"{BottomPanelName}/{imageName}");
            Image image = child.GetComponent<Image>();
            Require(image != null && image.sprite != null, imageName + " Sprite");
        }

        var serialized = new SerializedObject(bottomTabPanel);
        string[] fields =
        {
            "itemsButton",
            "shopButton",
            "itemsContent",
            "shopContent",
        };
        foreach (string field in fields)
        {
            Require(
                serialized.FindProperty(field).objectReferenceValue != null,
                $"BottomTabPanel.{field}");
        }

        Require(FindRequired<ShopPanel>() != null, "ShopPanel");
        Require(FindRequired<ItemPanel>() != null, "ItemPanel");
    }

    private static void ValidateSpriteImports(
        string folder,
        bool includeSubfolders = true)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!includeSubfolders &&
                !string.Equals(
                    System.IO.Path.GetDirectoryName(path)
                        ?.Replace('\\', '/'),
                    folder.TrimEnd('/'),
                    StringComparison.Ordinal))
            {
                continue;
            }

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Require(importer != null, path + " TextureImporter");
            Require(importer.filterMode == FilterMode.Point, path + " Point filter");
            Require(!importer.mipmapEnabled, path + " mipmaps disabled");
        }
    }

    private static void ConfigureSpriteImports(
        string folder,
        bool includeSubfolders = true)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!includeSubfolders &&
                !string.Equals(
                    System.IO.Path.GetDirectoryName(path)
                        ?.Replace('\\', '/'),
                    folder.TrimEnd('/'),
                    StringComparison.Ordinal))
            {
                continue;
            }

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Require(importer != null, path + " TextureImporter");
            bool changed =
                importer.filterMode != FilterMode.Point ||
                importer.mipmapEnabled;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            if (changed)
            {
                importer.SaveAndReimport();
            }
        }
    }

    private static void RestoreFunctionalObjects(
        StatusPanel statusPanel,
        WavePanel wavePanel,
        BottomTabPanel bottomTabPanel)
    {
        var statusSerialized = new SerializedObject(statusPanel);
        RestoreReferenceParent(
            statusSerialized,
            "playerHpText",
            statusPanel.transform);
        RestoreReferenceParent(
            statusSerialized,
            "goldText",
            statusPanel.transform);

        var waveSerialized = new SerializedObject(wavePanel);
        RestoreReferenceParent(
            waveSerialized,
            "startButton",
            wavePanel.transform);
        RestoreReferenceParent(
            waveSerialized,
            "launchButton",
            wavePanel.transform);

        var bottomSerialized = new SerializedObject(bottomTabPanel);
        RestoreReferenceParent(
            bottomSerialized,
            "itemsButton",
            bottomTabPanel.transform);
        RestoreReferenceParent(
            bottomSerialized,
            "shopButton",
            bottomTabPanel.transform);
        RestoreReferenceParent(
            bottomSerialized,
            "itemsContent",
            bottomTabPanel.transform);
        RestoreReferenceParent(
            bottomSerialized,
            "shopContent",
            bottomTabPanel.transform);
    }

    private static void RestoreReferenceParent(
        SerializedObject serialized,
        string fieldName,
        Transform parent)
    {
        UnityEngine.Object reference = serialized
            .FindProperty(fieldName).objectReferenceValue;
        if (reference is Component component)
        {
            component.transform.SetParent(parent, false);
        }
        else if (reference is GameObject gameObject)
        {
            gameObject.transform.SetParent(parent, false);
        }
    }

    private static void ConfigureButton(
        Button button,
        RectTransform parent,
        float x,
        float y,
        float width,
        float height,
        string normalAsset,
        string pressedAsset,
        string disabledAsset)
    {
        button.transform.SetParent(parent, false);
        Place((RectTransform)button.transform, x, y, width, height);
        Image image = button.targetGraphic as Image;
        Require(image != null, button.name + " target Image");
        Sprite normal = LoadHudSprite(normalAsset);
        image.sprite = normal;
        image.preserveAspect = false;
        button.transition = Selectable.Transition.SpriteSwap;
        SpriteState state = button.spriteState;
        state.highlightedSprite = normal;
        state.selectedSprite = normal;
        state.pressedSprite = LoadHudSprite(pressedAsset);
        state.disabledSprite = LoadHudSprite(disabledAsset);
        button.spriteState = state;
    }

    private static void ConfigureHudText(
        TextMeshProUGUI text,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color32(244, 204, 139, 255);
        text.enableAutoSizing = false;
        text.raycastTarget = false;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        RectTransform parent,
        TMP_FontAsset font)
    {
        RectTransform transform = CreateRectTransform(name, parent);
        var text = transform.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        return text;
    }

    private static Image CreateImage(string name, RectTransform parent)
    {
        RectTransform transform = CreateRectTransform(name, parent);
        return transform.gameObject.AddComponent<Image>();
    }

    private static RectTransform CreateRectTransform(
        string name,
        Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        var transform = (RectTransform)gameObject.transform;
        transform.SetParent(parent, false);
        return transform;
    }

    private static void ReparentAndPlace(
        RectTransform transform,
        RectTransform parent,
        float x,
        float y,
        float width,
        float height)
    {
        transform.SetParent(parent, false);
        transform.gameObject.SetActive(true);
        Place(transform, x, y, width, height);
    }

    private static void Place(
        RectTransform transform,
        float x,
        float y,
        float width,
        float height)
    {
        transform.anchorMin = new Vector2(0f, 0.5f);
        transform.anchorMax = new Vector2(0f, 0.5f);
        transform.pivot = new Vector2(0.5f, 0.5f);
        transform.anchoredPosition = new Vector2(x, y);
        transform.sizeDelta = new Vector2(width, height);
        transform.localScale = Vector3.one;
    }

    private static void PlaceAnchored(
        RectTransform transform,
        Vector2 anchor,
        Vector2 position,
        Vector2 size)
    {
        transform.anchorMin = anchor;
        transform.anchorMax = anchor;
        transform.pivot = anchor;
        transform.anchoredPosition = position;
        transform.sizeDelta = size;
        transform.localScale = Vector3.one;
    }

    private static void Stretch(
        RectTransform transform,
        float left = 0f,
        float bottom = 0f,
        float right = 0f,
        float top = 0f)
    {
        transform.anchorMin = Vector2.zero;
        transform.anchorMax = Vector2.one;
        transform.offsetMin = new Vector2(left, bottom);
        transform.offsetMax = new Vector2(-right, -top);
        transform.localScale = Vector3.one;
    }

    private static void AssignObject(
        SerializedObject serialized,
        string fieldName,
        UnityEngine.Object value)
    {
        serialized.FindProperty(fieldName).objectReferenceValue = value;
    }

    private static void AssignObjectArray<T>(
        SerializedObject serialized,
        string fieldName,
        T[] values)
        where T : UnityEngine.Object
    {
        SerializedProperty property = serialized.FindProperty(fieldName);
        property.arraySize = values.Length;
        for (int index = 0; index < values.Length; index++)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue =
                values[index];
        }
    }

    private static void ValidateObjectArray(
        SerializedObject serialized,
        string fieldName,
        int expectedCount)
    {
        SerializedProperty property = serialized.FindProperty(fieldName);
        Require(
            property != null && property.arraySize == expectedCount,
            $"{fieldName}[{expectedCount}]");
        for (int index = 0; index < expectedCount; index++)
        {
            Require(
                property.GetArrayElementAtIndex(index)
                    .objectReferenceValue != null,
                $"{fieldName}[{index}]");
        }
    }

    private static Sprite LoadHudSprite(string fileName)
    {
        return LoadSprite(HudAssetPath + fileName);
    }

    private static Sprite LoadUiSprite(string fileName)
    {
        return LoadSprite(UiAssetPath + fileName);
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        Require(sprite != null, path);
        return sprite;
    }

    private static T FindRequired<T>() where T : UnityEngine.Object
    {
        T result = UnityEngine.Object.FindFirstObjectByType<T>(
            FindObjectsInactive.Include);
        Require(result != null, typeof(T).Name);
        return result;
    }

    private static void DestroyGeneratedRoot(string name)
    {
        GameObject root = GameObject.Find(name);
        if (root != null)
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void Require(bool condition, string requirement)
    {
        if (!condition)
        {
            throw new InvalidOperationException(
                $"[ArcaneGameUiSetup] Missing requirement: {requirement}");
        }
    }
}
#endif
