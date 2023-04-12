using Godot;

public partial class BulletItem : AmmoItem
{
    [Export] public PackedScene ProjectileScene {get; set;}

    public override void Fire(Vector3 origin, Vector3 velocity, Combatant user, GunItem gun)
    {
        Projectile proj = user.World.Entities.SpawnObject<Projectile>(ProjectileScene, origin);
        proj.Damage = Damage+gun?.Damage ?? Damage;
        proj.Launch(velocity, user.Team);
    }
}