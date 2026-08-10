#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class ShopSlotVisualTests
{
    [TestCase(EItemCategory.Ball, 0)]
    [TestCase(EItemCategory.Board, 1)]
    [TestCase(EItemCategory.Battle, 2)]
    public void ResolveNormalSprite_ReturnsCategorySprite(
        EItemCategory category,
        int expectedIndex)
    {
        var sprites = new[]
        {
            CreateSprite(),
            CreateSprite(),
            CreateSprite()
        };

        var result = ShopSlot.ResolveNormalSprite(
            category,
            sprites[0],
            sprites[1],
            sprites[2]);

        Assert.That(result, Is.SameAs(sprites[expectedIndex]));
    }

    [Test]
    public void ResolveNormalSprite_MissingCategorySpriteFallsBackToBall()
    {
        var ball = CreateSprite();

        var result = ShopSlot.ResolveNormalSprite(
            EItemCategory.Board,
            ball,
            null,
            null);

        Assert.That(result, Is.SameAs(ball));
    }

    private static Sprite CreateSprite()
    {
        return Sprite.Create(
            new Texture2D(1, 1),
            new Rect(0f, 0f, 1f, 1f),
            Vector2.zero);
    }
}
#endif
