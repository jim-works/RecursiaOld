using Godot;

public class ChainLink : SegmentedCombatantChild
{
    [Export]
    public float Tension = 1;
    [Export]
    public float NaturalDist=2;

    public override void _PhysicsProcess(float dt)
    {
        if (Parent == null || Parent.IsQueuedForDeletion()) {
            QueueFree();
            return;
        }
        //tension force towards/away from parent
        Vector3 d = Parent.Position-Position;
        AddForce(d.Normalized()*Tension*(d.Length()-NaturalDist));

        base._PhysicsProcess(dt);
    }
}