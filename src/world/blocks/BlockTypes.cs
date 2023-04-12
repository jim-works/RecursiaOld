using System.Collections.Generic;
using Godot;

namespace Recursia;
public static class BlockTypes
{
    private static readonly Dictionary<string, System.Func<Block>> blocks = new();

    public static Block Get(string blockName) {
        if (blocks.TryGetValue(blockName, out var b)) return b();
        Godot.GD.PushWarning($"Block {blockName} not found");
        return null;
    }
    public static System.Func<Block> GetFactory(string blockName) {
        if (blocks.TryGetValue(blockName, out var b)) return b;
        Godot.GD.PushWarning($"Block factory {blockName} not found");
        return null;
    }

    public static void CreateType(string name, System.Func<Block> factory) {
        if (blocks.ContainsKey(name)) {
            Godot.GD.PushWarning($"Block {name} already exists, replacing!");
        }
        Godot.GD.Print($"Created block type {name}");
        blocks[name] = factory;
    }
}