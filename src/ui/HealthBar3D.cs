using Godot;
using System;

public class HealthBar3D : Spatial
{
    public override void _EnterTree()
    {
        base._EnterTree();
        GetNode<HealthBar>("Viewport/HealthBar").Tracking = GetParent<Combatant>();
    }

    public override void _Process(float delta)
    {
        LookAt(RotatingCamera.Singleton.GlobalTransform.origin, RotatingCamera.Singleton.GlobalTransform.basis.y);
        base._Process(delta);
    }
}
