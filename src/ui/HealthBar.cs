using Godot;
using System;

public partial class HealthBar : Node
{
    [Export]
    public Combatant Tracking;

    private ColorRect healthbar;

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
        float proportion = Tracking.GetHealth()/Tracking.GetMaxHealth();
        healthbar.AnchorRight = proportion;
    }
}
