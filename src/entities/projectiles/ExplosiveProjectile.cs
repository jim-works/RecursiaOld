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
        SphereShaper.Shape3D(World, GlobalPosition, ExplosionSize);
        GpuParticles3D TrailParticles = GetNode<GpuParticles3D>("Trail");
        GpuParticles3D ExplosionParticles = GetNode<GpuParticles3D>("Explosion");
        ExplosionParticles.Emitting = true;
        TrailParticles.Emitting = false;
        PhysicsActive = false;
        dying = true;
        foreach(PhysicsObject obj in World.Entities.GetPhysicsObjectsInRange(GlobalPosition,ExplosionSize))
        {
            if (obj == this) continue;
            obj.AddImpulse((Vector3.Up+(obj.GlobalPosition-GlobalPosition).Normalized())*FlingFactor);
        }
        foreach(Combatant obj in World.Entities.GetEnemiesInRange(GlobalPosition,ExplosionSize,team))
        {
            obj.TakeDamage(new Damage{Team=team,Amount=Damage});
        }
    }
}