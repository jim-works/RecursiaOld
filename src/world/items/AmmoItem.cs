using Godot;

namespace Recursia;
public abstract partial class AmmoItem : Item
{
    [Export] public int Damage;

    protected AmmoItem(string typeName, string displayname) : base(typeName, displayname) {}

    public abstract void Fire(Vector3 origin, Vector3 velocity, Combatant user, GunItem gun);
}