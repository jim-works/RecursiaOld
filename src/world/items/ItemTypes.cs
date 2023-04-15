using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Recursia;
public static class ItemTypes
{
    private static readonly Dictionary<string, Item> items = new();
    public static bool TryGet(string itemName, [MaybeNullWhen(false)] out Item item) => items.TryGetValue(itemName, out item);

    //looks up <blockName> then creates an item that places that block
    public static bool TryGetBlockItem(string blockName, [MaybeNullWhen(false)] out BlockFactoryItem f, float reach = 10)
    {
        if (BlockTypes.TryGet(blockName, out Block? b))
        {
            f = new BlockFactoryItem($"blockFactoryItem:{blockName}", blockName, reach) {
                DisplayName = blockName,
                Texture2D = b.ItemTexture
            };
            return true;
        }
        f = null;
        return false;
    }

    //creates an item that will place <block>
    //should only be used for picking up blocks with data you want to keep
    //the string overload will automatically create a factory/use the cached block pointer as required
    public static BlockItem GetBlockItem(Block b) => new($"blockItem:{b.Name}", b.Name, b)
    {
        Placing = b,
        Texture2D = b.ItemTexture
    };

    public static void CreateType(string name, Item item) {
        if (items.ContainsKey(name)) {
            Godot.GD.PushWarning($"Item {name} already exists, replacing!");
        }
        Godot.GD.Print($"Created item type {name} with item name {item.DisplayName}");
        if (item.TypeName != name)
        {
            Godot.GD.PushError($"requested to create item with type {name} does not match actual item's TypeName {item.TypeName}");
            return;
        }
        items[name] = item;
    }
}