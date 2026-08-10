#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class GameShopUiSceneTests
{
    private const string UiPath = "Assets/03. Images/UI/";

    [Test]
    public void GameScene_UsesNewShopAndItemUiSprites()
    {
        EditorSceneManager.OpenScene("Assets/01. Scenes/02. Game.unity");

        var bottomTabs = Object.FindFirstObjectByType<BottomTabPanel>(
            FindObjectsInactive.Include);
        var shopPanel = Object.FindFirstObjectByType<ShopPanel>(
            FindObjectsInactive.Include);
        Assert.That(bottomTabs, Is.Not.Null);
        Assert.That(shopPanel, Is.Not.Null);

        var itemButton = ReadReference<Button>(bottomTabs, "itemsButton");
        var shopButton = ReadReference<Button>(bottomTabs, "shopButton");
        var rerollButton = ReadReference<Button>(shopPanel, "rerollButton");

        var sideInactive = LoadSprite("ui_side_tab_inactive.png");
        var sidePressed = LoadSprite("ui_side_tab_pressed.png");
        var sideActive = LoadSprite("ui_side_tab_active.png");
        AssertButtonSprites(itemButton, sideInactive, sideInactive, sidePressed, sideActive);
        AssertButtonSprites(shopButton, sideInactive, sideInactive, sidePressed, sideActive);

        Assert.That(
            ReadReference<Image>(bottomTabs, "itemsIconImage").sprite,
            Is.SameAs(LoadSprite("ui_icon_item_tab.png")));
        Assert.That(
            ReadReference<Image>(bottomTabs, "shopIconImage").sprite,
            Is.SameAs(LoadSprite("ui_icon_shop_tab.png")));

        AssertButtonSprites(
            rerollButton,
            LoadSprite("ui_button_shop_reroll_normal.png"),
            LoadSprite("ui_button_shop_reroll_highlighted.png"),
            LoadSprite("ui_button_shop_reroll_pressed.png"),
            LoadSprite("ui_button_shop_reroll_disabled.png"));

        var tooltipBackground = LoadSprite("ui_item_tooltip_background.png");
        var shopTooltip = Object.FindFirstObjectByType<ShopTooltip>(
            FindObjectsInactive.Include);
        var itemTooltip = Object.FindFirstObjectByType<ItemTooltip>(
            FindObjectsInactive.Include);
        Assert.That(shopTooltip.GetComponent<Image>().sprite, Is.SameAs(tooltipBackground));
        Assert.That(itemTooltip.GetComponent<Image>().sprite, Is.SameAs(tooltipBackground));

        var ball = LoadSprite("ui_shop_slot_ball_normal.png");
        var board = LoadSprite("ui_shop_slot_board_normal.png");
        var battle = LoadSprite("ui_shop_slot_battle_normal.png");
        var highlighted = LoadSprite("ui_shop_slot_highlighted.png");
        var soldOut = LoadSprite("ui_shop_slot_sold_out.png");
        var slots = Object.FindObjectsByType<ShopSlot>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        Assert.That(slots, Is.Not.Empty);

        foreach (var slot in slots)
        {
            Assert.That(ReadReference<Sprite>(slot, "ballNormalSprite"), Is.SameAs(ball));
            Assert.That(ReadReference<Sprite>(slot, "boardNormalSprite"), Is.SameAs(board));
            Assert.That(ReadReference<Sprite>(slot, "battleNormalSprite"), Is.SameAs(battle));

            var purchaseButton = ReadReference<Button>(slot, "purchaseButton");
            Assert.That(purchaseButton.spriteState.highlightedSprite, Is.SameAs(highlighted));
            Assert.That(purchaseButton.spriteState.pressedSprite, Is.SameAs(highlighted));
            Assert.That(purchaseButton.spriteState.disabledSprite, Is.SameAs(soldOut));
        }
    }

    private static void AssertButtonSprites(
        Button button,
        Sprite normal,
        Sprite highlighted,
        Sprite pressed,
        Sprite disabled)
    {
        Assert.That(button.transition, Is.EqualTo(Selectable.Transition.SpriteSwap));
        Assert.That(button.image.sprite, Is.SameAs(normal));
        Assert.That(button.spriteState.highlightedSprite, Is.SameAs(highlighted));
        Assert.That(button.spriteState.pressedSprite, Is.SameAs(pressed));
        Assert.That(button.spriteState.disabledSprite, Is.SameAs(disabled));
    }

    private static Sprite LoadSprite(string fileName)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiPath + fileName);
        Assert.That(sprite, Is.Not.Null, fileName);
        return sprite;
    }

    private static T ReadReference<T>(Object target, string propertyName)
        where T : Object
    {
        var property = new SerializedObject(target).FindProperty(propertyName);
        Assert.That(property, Is.Not.Null, propertyName);
        return property.objectReferenceValue as T;
    }
}
#endif
