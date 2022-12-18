using Godot;

public class ChainLink : Combatant
{
    [Export]
    public float Tension = 1;
    [Export]
    public float NaturalDist=2;

    public PhysicsObject Parent;

    public override void _PhysicsProcess(float dt)
    {
        //tension force towards/away from parent
        Vector3 d = Parent.Position-Position;
        AddForce(d.Normalized()*Tension*(d.Length()-NaturalDist));
        base._PhysicsProcess(dt);
    }
}