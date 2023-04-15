using Godot;

namespace Recursia;
public partial class BulletItem : AmmoItem
{
    [Export] public PackedScene? ProjectileScene {get; set;}

    public BulletItem(string typeName, string displayname) : base(typeName, displayname) {}

    public override void Fire(Vector3 origin, Vector3 velocity, Combatant user, GunItem gun)
    {
        if (ProjectileScene == null)
        {
            GD.PushWarning($"ProjectileScene null on BulletItem {TypeName}");
            return;
        }
        Projectile proj = user.World!.Entities.SpawnObject<Projectile>(ProjectileScene, origin);
        proj.Damage = Damage+gun?.Damage ?? Damage;
        proj.Launch(velocity, user.Team);
    }
}