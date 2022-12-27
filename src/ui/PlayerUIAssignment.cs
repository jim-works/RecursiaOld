using Godot;

public class PlayerUIAssignment : Node
{
    public override void _Ready()
    {
        GetNode<HealthBar>("HealthBar").Tracking = World.Singleton.Players[0];
        base._Ready();
    }
}