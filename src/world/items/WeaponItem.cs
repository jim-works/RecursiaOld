using Godot;

namespace Recursia;
public abstract partial class WeaponItem : Item
{
    [Export] public int Damage;

    protected WeaponItem(string typeName, string displayname) : base(typeName, displayname){}
}