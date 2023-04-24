using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Recursia;
public static class ItemTypes
{
    private static readonly Dictionary<string, Func<Item>> items = new();
    private static readonly Dictionary<string, BlockFactoryItem> blockFactoryItemCache = new();

    static ItemTypes()
    {
        CreateType("blockFactoryItem", () => new BlockFactoryItem("blockFactoryItem"));
        CreateType("blockItem", () => new BlockItem("blockItem"));
    }
    public static bool TryGet(string itemName, [MaybeNullWhen(false)] out Item item) {
        if (items.TryGetValue(itemName, out Func<Item>? i)) {
            item = i();
            return true;
        }
        item = null;
        return false;
    }
    public static BlockFactoryItem? GetBlockFactoryItem(string factoryName)
    {
        if (blockFactoryItemCache.TryGetValue(factoryName, out BlockFactoryItem? fact)) return fact;
        //first time this is being used, must create new factory
        if (TryGet("blockFactoryItem", out Item? item))
        {
            BlockFactoryItem blockFact = (BlockFactoryItem)item;
            blockFact.FactoryName = factoryName;
            blockFactoryItemCache[factoryName] = blockFact;
            return blockFact;
        }
        return null;
    }
    public static BlockItem? GetBlockItem(Block b)
    {
        if (TryGet("blockItem", out Item? item))
        {
            BlockItem blockItem = (BlockItem)item;
            blockItem.Placing = b;
            return blockItem;
        }
        return null;
    }

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
        items[name] = () => item;
    }
        public static void CreateType(string name, Func<Item> item) {
        if (items.ContainsKey(name)) {
            Godot.GD.PushWarning($"Item {name} already exists, replacing!");
        }
        Godot.GD.Print($"Created item type {name}");
        items[name] = item;
    }
}