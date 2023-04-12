using Godot;
using System;

namespace Recursia;
public partial class CoordinateTextUI : Label
{
    [Export]
    public Combatant Tracking;

    public override void _Process(double delta)
    {
        if (Tracking == null) {
            return;
        }
        Text = $"{Tracking.GlobalPosition.ToString("F2")}\n{(BlockCoord)Tracking.GlobalPosition}\n{(ChunkCoord)Tracking.GlobalPosition}";
    }
}
