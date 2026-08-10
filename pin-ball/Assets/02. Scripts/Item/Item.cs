using UnityEngine;

public class Item
{
    public string Id { get; }
    public EItem Key { get; }
    public EItemCategory Category { get; }

    public string Name { get; }
    public string Description { get; }
    public int Cost { get; }
    public Sprite Icon { get; }

    public float Value1 { get; }
    public float Value2 { get; }
    public float Value3 { get; }

    public Item(ItemData data, Sprite icon)
    {
        Id = data.id;
        Key = (EItem)data.key;
        Category = (EItemCategory)data.type;

        Value1 = data.value1;
        Value2 = data.value2;
        Value3 = data.value3;

        Cost = data.cost;
        Name = data.name;
        Description = CreateDescription(data.desc);
        Icon = icon;
    }
    
    private string CreateDescription(string format)
    {
        return string.Format(
            format,
            Value1 >= 1 ? Value1 : ToPercent(Value1),
            Value2 >= 1 ? Value2 : ToPercent(Value2),
            Value3 >= 1 ? Value3 : ToPercent(Value3));
    }

    private static string ToPercent(float value)
    {
        return $"{value * 100:0.##}";
    }
}