using Godot;

public partial class GunItem : WeaponItem
{
    public float ShootSpeed = 50;
    public int AmmoPerShot=1;

    public override void OnUse(Combatant user, Vector3 position, Vector3 dir, ref ItemStack source)
    {
        //check for ammo
        if (user.Inventory == null) return;
        int bulletSlot = user.Inventory.Select(stack => stack.Size >= AmmoPerShot && stack.Item is BulletItem);
        if (bulletSlot == -1) return;

        //Get bullet and update inventory
        BulletItem bullet = (BulletItem)user.Inventory.Items[bulletSlot].Item;
        user.Inventory.DeleteFromSlot(bulletSlot,AmmoPerShot);

        onFire(bullet,user,position,dir,ref source);

        base.OnUse(user, position, dir, ref source);
    }

    protected virtual void onFire(BulletItem bullet, Combatant user, Vector3 position, Vector3 dir, ref ItemStack source)
    {
        bullet.Fire(position, dir*ShootSpeed, user, this);
    }
}