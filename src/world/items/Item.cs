using Godot;
using System.IO;

#pragma warning disable CS0660, CS0661 //intentionally not overriding gethashcode or .equals here
//TODO: submit issue to fix godot import bug with c# plugins
//[MonoCustomResourceRegistry.RegisteredType(nameof(Item), "", nameof(Resource))]
namespace Recursia;
public partial class Item : Resource, ISerializable
{
    public static readonly Item Empty = new("empty") {MaxStack = 0};
    [Export] public string DisplayName;
    [Export] public int MaxStack = 999;
    [Export] public float Cooldown = 0;
    [Export] public Texture2D? Texture2D;
    [Export] public AudioStream? UseSound;

    public string TypeName {get;}

    public Item(string typeName)
    {
        TypeName = typeName;
        DisplayName = typeName;
    }
    public Item(string typeName, string displayName)
    {
        TypeName = typeName;
        DisplayName = displayName;
    }

    public virtual bool OnUse(Combatant user, Vector3 position, Vector3 dir, ref ItemStack source)
    {
        user.ItemCooldown = Cooldown;
        return true;
    }

    public virtual void Serialize(BinaryWriter bw)
    {
        bw.Write(DisplayName);
    }
    public virtual void Deserialize(BinaryReader br)
    {
        DisplayName = br.ReadString();
    }

    //allows subclasses to override .Equals, keeping == and != consistent with that
    public static bool operator ==(Item? a, Item? b)
    {
        return a is null ? b is null : a.Equals(b);
    }
    public static bool operator !=(Item? a, Item? b) => !(a==b);
}