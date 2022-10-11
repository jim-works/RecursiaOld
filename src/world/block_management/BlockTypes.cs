using System.Collections.Generic;

public static class BlockTypes
{
    private static Dictionary<string, System.Func<Block>> blocks = new Dictionary<string, System.Func<Block>>();

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

    public static void CreateBlockType(string name, System.Func<Block> factory) {
        if (blocks.ContainsKey(name)) {
            Godot.GD.PushWarning($"Block {name} already exists, replacing!");
        }
        Godot.GD.Print($"Created block type {name}");
        blocks[name] = factory;
    }
}