using Godot;
using System;

namespace Recursia;
public partial class HealthBar : Node
{
    [Export]
    public Combatant? Tracking;

    private ColorRect healthbar = null!;

    public override void _Ready()
    {
        base._Ready();
        healthbar = GetNode<ColorRect>("Health");
    }

    public override void _Process(double delta)
    {
        if (Tracking == null) {
            return;
        }
        healthbar.AnchorRight = Tracking.GetHealth()/Tracking.GetMaxHealth();
    }
}
