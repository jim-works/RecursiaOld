using Godot;

public class GunItem : Item
{
    public PackedScene Projectile = GD.Load<PackedScene>("res://objects/Bullet.tscn");
    public float ShootSpeed = 50;

    public override void OnUse(Combatant user, Vector3 position, Vector3 dir)
    {
        Projectile proj = Projectile.Instance<Projectile>();
        World.Singleton.AddChild(proj);
        proj.Position = position;
        proj.Launch(dir*ShootSpeed, user.Team);
        base.OnUse(user, position, dir);
    }
}