using Godot;

public class ChainLink : Combatant
{
    [Export]
    public float Tension = 1;
    [Export]
    public float NaturalDist=2;

    public PhysicsObject AttachedTo;

    public override void _PhysicsProcess(float dt)
    {
        if (AttachedTo == null || AttachedTo.IsQueuedForDeletion()) {
            QueueFree();
            return;
        }
        //tension force towards/away from parent
        Vector3 d = AttachedTo.Position-Position;
        AddForce(d.Normalized()*Tension*(d.Length()-NaturalDist));

        base._PhysicsProcess(dt);
    }
}