using Godot;
using System;

public class CoordinateTextUI : Label
{
    [Export]
    public Combatant Tracking;

    public override void _Process(float delta)
    {
        if (Tracking == null) {
            return;
        }
        Text = $"{Tracking.Position.ToString("F2")}\n{(BlockCoord)Tracking.Position}\n{(ChunkCoord)Tracking.Position}";
    }
}
