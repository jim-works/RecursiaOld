using Godot;

namespace Recursia;
public partial class Projectile : PhysicsObject
{
    [Export]
    public double Lifetime = 10;
    [Export]
    public float Damage = 5;
    protected Team? team;
    protected Vector3 launchVelocity;
    protected float launchSpeed;

    public void Launch(Vector3 startPos, Vector3 velocity, Team? team)
    {
        this.team = team;
        Velocity = velocity;
        launchVelocity = velocity;
        launchSpeed = velocity.Length();
        LookAtFromPosition(startPos, startPos + velocity);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (World?.Entities.CollidesWithEnemy(GetBox(), team) is Combatant hit)
        {
            onHit(hit);
        }
        Lifetime -= delta;
        if (Lifetime <= 0) {
            onHit(null);
        }
    }

    protected virtual void onHit(Combatant? c)
    {
        c?.TakeDamage(new Damage{Amount=Damage,Team=team});
        QueueFree();
    }
}