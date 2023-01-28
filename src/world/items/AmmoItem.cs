using Godot;

public abstract class AmmoItem : Item
{
    public int Damage;

    public abstract void Fire(Vector3 origin, Vector3 velocity, Combatant user, GunItem gun);
}