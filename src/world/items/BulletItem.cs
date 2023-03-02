using Godot;

public partial class BulletItem : AmmoItem
{
    public PackedScene ProjectileScene;

    public override void Fire(Vector3 origin, Vector3 velocity, Combatant user, GunItem gun)
    {
        Projectile proj = ProjectileScene.Instantiate<Projectile>();
        proj.Damage = Damage+gun?.Damage ?? Damage;
        World.Singleton.AddChild(proj);
        proj.GlobalPosition = origin;
        proj.Launch(velocity, user.Team);
    }
}