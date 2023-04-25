using Godot;

namespace Recursia;
public partial class ChainLink : SegmentedCombatantChild
{
    [Export]
    public float Tension = 1;
    [Export]
    public float NaturalDist=2;

    public override void _PhysicsProcess(double delta)
    {
        if (Parent == null || !IsInstanceValid(Parent)) {
            QueueFree();
            return;
        }
        //tension force towards/away from parent
        if (!Parent.IsInsideTree()) return;
        Vector3 d = Parent.GlobalPosition-GlobalPosition;
        AddConstantForce(d.Normalized()*Tension*(d.Length()-NaturalDist));

        base._PhysicsProcess(delta);
    }
}