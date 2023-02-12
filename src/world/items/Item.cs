using Godot;
using System.IO;

#pragma warning disable CS0660, CS0661 //intentionally not overriding gethashcode or .equals here
public class Item : ISerializable
{
    public string Name;
    public int MaxStack = 999;
    public float Cooldown = 0;
    public Texture Texture;
    public AudioStream UseSound;
    
    public virtual void OnUse(Combatant user, Vector3 position, Vector3 dir, ref ItemStack source)
    {
        user.ItemCooldown = Cooldown;
    }

    public virtual void Serialize(BinaryWriter bw)
    {
        bw.Write(Name);
    }

    //allows subclasses to override .Equals, keeping == and != consistent with that
    public static bool operator ==(Item a, Item b)
    {
        return Object.ReferenceEquals(a,null) ? Object.ReferenceEquals(b,null) : a.Equals(b);
    }
    public static bool operator !=(Item a, Item b) => !(a==b);
}