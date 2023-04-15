using Godot;

namespace Recursia;
public partial class TrackingProjectile : Projectile
{
    [Export] public double TrackingDelay;
    [Export] public float TrackSpeedMult = 1.5f;
    [Export] public float AggroRange = 50;
    private Combatant? target;

    public override void _PhysicsProcess(double delta)
    {
        TrackingDelay -= delta;
        if (target == null && TrackingDelay <= 0)
        {
            World!.Entities.ClosestEnemy(GlobalPosition, team, AggroRange, out target);
        }
        if (target != null)
        {
            Velocity = launchSpeed*TrackSpeedMult*(target.GlobalPosition-GlobalPosition).Normalized();
        }

        base._PhysicsProcess(delta);
    }
}