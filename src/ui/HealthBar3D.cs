using Godot;
using System;

public class HealthBar3D : Node
{
    public override void _EnterTree()
    {
        base._EnterTree();
        GetNode<HealthBar>("Viewport/HealthBar").Tracking = GetParent<Combatant>();
    }
}
