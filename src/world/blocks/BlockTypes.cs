using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Recursia;
public static class BlockTypes
{
    private static readonly Dictionary<string, System.Func<Block>> blocks = new();

    public static bool TryGet(string blockName, [MaybeNullWhen(false)] out Block b) {
        if (blocks.TryGetValue(blockName, out var f))
        {
            b = f();
            return true;
        }
        b = null;
        return false;
    }
    public static bool TryGetFactory(string blockName, [MaybeNullWhen(false)] out System.Func<Block> f) => blocks.TryGetValue(blockName, out f);

    public static void CreateType(string name, System.Func<Block> factory) {
        if (blocks.ContainsKey(name)) {
            Godot.GD.PushWarning($"Block {name} already exists, replacing!");
        }
        Godot.GD.Print($"Created block type {name}");
        blocks[name] = factory;
    }
}