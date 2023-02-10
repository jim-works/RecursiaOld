using Godot;
using System;

public class HealthBar3D : Spatial
{
    public override void _Ready()
    {
        base._Ready();
        GetNode<HealthBar>("Viewport/HealthBar").Tracking = GetParent<Combatant>();
    }
}
