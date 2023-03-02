using Godot;
using System;

public partial class HealthBar3D : Node3D
{
    public override void _Ready()
    {
        base._Ready();
        GetNode<HealthBar>("SubViewport/HealthBar").Tracking = GetParent<Combatant>();
    }
}
