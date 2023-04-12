using Godot;

namespace Recursia;
public partial class ShotgunItem : GunItem
{
    [Export] public float Spread = 0.05f;
    [Export] public int BulletsPerShot = 5;

    protected override void onFire(BulletItem bullet, Combatant user, Vector3 position, Vector3 dir, ref ItemStack source)
    {
        for (int i = 0; i < BulletsPerShot; i++)
        {
            Vector3 randDir = new Vector3(dir.X+(2*GD.Randf()+1)*Spread,dir.Y+(2*GD.Randf()+1)*Spread,dir.Z+(2*GD.Randf()+1)*Spread).Normalized();
            bullet.Fire(position, randDir*ShootSpeed, user, this);
        }
    }
}