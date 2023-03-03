using Godot;

public abstract partial class AmmoItem : Item
{
    [Export] public int Damage;

    public abstract void Fire(Vector3 origin, Vector3 velocity, Combatant user, GunItem gun);
}