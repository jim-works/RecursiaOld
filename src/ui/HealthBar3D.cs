using Godot;
using System;

public partial class HealthBar3D : Node3D
{
    [Export] public HealthBar SubBar;
    public override void _Ready()
    {
        SubBar.Tracking = GetParent<Combatant>();
        base._Ready();
    }
}
