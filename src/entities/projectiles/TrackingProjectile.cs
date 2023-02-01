using Godot;

public class TrackingProjectile : Projectile
{
    [Export] public float TrackingDelay;
    [Export] public float TrackSpeedMult = 1.5f;
    private Combatant target;

    public override void _PhysicsProcess(float delta)
    {
        TrackingDelay -= delta;
        if (target == null && TrackingDelay <= 0)
        {
            World.Singleton.ClosestEnemy(Position, team, out target);
        }
        if (target != null)
        {
            Velocity = launchSpeed*TrackSpeedMult*(target.Position-Position).Normalized();
        }
        
        base._PhysicsProcess(delta);
    }
}