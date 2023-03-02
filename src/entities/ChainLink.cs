using Godot;

public partial class ChainLink : SegmentedCombatantChild
{
    [Export]
    public float Tension = 1;
    [Export]
    public float NaturalDist=2;

    public override void _PhysicsProcess(double dt)
    {
        if (Parent == null || !IsInstanceValid(Parent)) {
            QueueFree();
            return;
        }
        //tension force towards/away from parent
        Vector3 d = Parent.GlobalPosition-GlobalPosition;
        AddConstantForce(d.Normalized()*Tension*(d.Length()-NaturalDist));

        base._PhysicsProcess(dt);
    }
}