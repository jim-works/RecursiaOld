using Godot;

public partial class TrackingProjectile : Projectile
{
    [Export] public double TrackingDelay;
    [Export] public float TrackSpeedMult = 1.5f;
    private Combatant target;

    public override void _PhysicsProcess(double delta)
    {
        TrackingDelay -= delta;
        if (target == null && TrackingDelay <= 0)
        {
            World.Singleton.ClosestEnemy(GlobalPosition, team, out target);
        }
        if (target != null)
        {
            Velocity = launchSpeed*TrackSpeedMult*(target.GlobalPosition-GlobalPosition).Normalized();
        }
        
        base._PhysicsProcess(delta);
    }
}