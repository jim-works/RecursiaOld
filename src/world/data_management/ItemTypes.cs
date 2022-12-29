using System.Collections.Generic;

public static class ItemTypes
{
    private static Dictionary<string, Item> items = new Dictionary<string, Item>();

    public static Item Get(string itemName) {
        if (items.TryGetValue(itemName, out var b)) return b;
        Godot.GD.PushWarning($"Item {itemName} not found");
        return null;
    }

    public static void CreateType(string name, Item item) {
        if (items.ContainsKey(name)) {
            Godot.GD.PushWarning($"Item {name} already exists, replacing!");
        }
        Godot.GD.Print($"Created item type {name}");
        items[name] = item;
    }
}