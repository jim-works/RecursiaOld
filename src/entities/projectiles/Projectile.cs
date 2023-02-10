using Godot;

public class Projectile : PhysicsObject
{
    [Export]
    public float Lifetime = 10;
    [Export]
    public float Damage = 5;
    protected Team team;
    protected Vector3 launchVelocity;
    protected float launchSpeed;

    public void Launch(Vector3 velocity, Team team)
    {
        this.team = team;
        Velocity = velocity;
        launchVelocity = velocity;
        launchSpeed = velocity.Length();
        LookAt(Position + velocity, Vector3.Up);
    }

    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);
        if (World.Singleton.CollidesWithEnemy(GetBox(), team) is Combatant hit)
        {
            onHit(hit);
        }
        Lifetime -= delta;
        if (Lifetime <= 0) {
            onHit(null);
            return;
        }
    }

    protected virtual void onHit(Combatant c)
    {
        if (c!= null) c.TakeDamage(new Damage{Amount=Damage,Team=team});
        QueueFree();
    }
}