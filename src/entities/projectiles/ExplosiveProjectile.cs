using Godot;

public partial class ExplosiveProjectile : Projectile
{
    [Export] public float ExplosionSize = 10;
    [Export] public float FlingFactor = 1;
    [Export] public AudioStream ExplosionSound;
    [Export] public NodePath AudioPlayerPath;
    private bool exploded = false;
    private bool dying = false;
    private double dieTime = 2;
    private AudioStreamPlayer3D audioStreamPlayer;

    public override void _Ready()
    {
        audioStreamPlayer = GetNode<AudioStreamPlayer3D>(AudioPlayerPath);
        base._Ready();
    }

    public override void _PhysicsProcess(double delta)
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
        SphereShaper.Shape3D(World.Singleton, GlobalPosition, ExplosionSize);
        GpuParticles3D TrailParticles = GetNode<GpuParticles3D>("Trail");
        GpuParticles3D ExplosionParticles = GetNode<GpuParticles3D>("Explosion");
        ExplosionParticles.Emitting = true;
        TrailParticles.Emitting = false;
        PhysicsActive = false;
        dying = true;
        foreach(PhysicsObject obj in World.Singleton.PhysicsObjects)
        {
            if (obj == this) continue;
            float mag = (obj.GlobalPosition-GlobalPosition).LengthSquared()+1;
            obj.AddImpulse((obj.GlobalPosition-GlobalPosition)/mag*ExplosionSize*FlingFactor);
        }
        foreach(Combatant obj in World.Singleton.Combatants)
        {
            if ((obj.GlobalPosition-GlobalPosition).LengthSquared() <= ExplosionSize*ExplosionSize) obj.TakeDamage(new Damage{Team=team,Amount=Damage});
        }
    }
}