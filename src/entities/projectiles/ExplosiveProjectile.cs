using Godot;

public class ExplosiveProjectile : Projectile
{
    [Export] public float ExplosionSize = 10;
    [Export] public float FlingFactor = 1;
    [Export] public AudioStream ExplosionSound;
    [Export] public NodePath AudioPlayerPath;
    private bool exploded = false;
    private bool dying = false;
    private float dieTime = 2;
    private AudioStreamPlayer3D audioStreamPlayer;

    public override void _Ready()
    {
        audioStreamPlayer = GetNode<AudioStreamPlayer3D>(AudioPlayerPath);
        base._Ready();
    }

    public override void _PhysicsProcess(float delta)
    {
        if (!exploded && collisionDirections != 0)
        {
            onHit(null);
        }
        if (dying)
        {
            dieTime -= delta;
            if (dieTime <= 0) {
                QueueFree();
            }
        }
        base._PhysicsProcess(delta);
    }
    protected override void onHit(Combatant c)
    {
        if (exploded) return;
        Explode();
        if (c!= null) c.TakeDamage(new Damage{Amount=Damage,Team=team});
    }
    public void Explode()
    {
        audioStreamPlayer.Stream = ExplosionSound;
        audioStreamPlayer.Play();
        exploded = true;
        SphereShaper.Shape(World.Singleton, Position, ExplosionSize);
        Particles TrailParticles = GetNode<Particles>("Trail");
        Particles ExplosionParticles = GetNode<Particles>("Explosion");
        ExplosionParticles.Emitting = true;
        TrailParticles.Emitting = false;
        PhysicsActive = false;
        dying = true;
        foreach(PhysicsObject obj in World.Singleton.PhysicsObjects)
        {
            if (obj == this) continue;
            float mag = (obj.Position-Position).LengthSquared()+1;
            obj.AddImpulse((obj.Position-Position)/mag*ExplosionSize*FlingFactor);
        }
        foreach(Combatant obj in World.Singleton.Combatants)
        {
            if ((obj.Position-Position).LengthSquared() <= ExplosionSize*ExplosionSize) obj.TakeDamage(new Damage{Team=team,Amount=Damage});
        }
    }
}