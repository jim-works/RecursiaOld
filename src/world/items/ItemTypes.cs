using System.Collections.Generic;

public static class ItemTypes
{
    private static Dictionary<string, Item> items = new Dictionary<string, Item>();
    public static Item Get(string itemName) {
        if (items.TryGetValue(itemName, out var b)) return b;
        Godot.GD.PushWarning($"Item {itemName} not found");
        return null;
    }

    //looks up <blockName> then creates an item that places that block
    public static BlockFactoryItem GetBlockItem(string blockName)
    {
        Block b = BlockTypes.Get(blockName);
        if (b == null) return null; //invalid block
        return new BlockFactoryItem {
            BlockName = blockName,
            DisplayName = blockName,
            Texture2D = b.ItemTexture
        };
    }

    //creates an item that will place <block>
    //should only be used for picking up blocks with data you want to keep
    //the string overload will automatically create a factory/use the cached block pointer as required
    public static BlockItem GetBlockItem(Block b) => new BlockItem {
            DisplayName=b.Name,
            Placing=b,
            Texture2D = b.ItemTexture
        };

    public static void CreateType(string name, Item item) {
        if (items.ContainsKey(name)) {
            Godot.GD.PushWarning($"Item {name} already exists, replacing!");
        }
        Godot.GD.Print($"Created item type {name} with item name {item.DisplayName}");
        item.TypeName = name;
        items[name] = item;
    }
}