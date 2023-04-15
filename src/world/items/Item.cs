using Godot;
using System.IO;

#pragma warning disable CS0660, CS0661 //intentionally not overriding gethashcode or .equals here
//TODO: submit issue to fix godot import bug with c# plugins
//[MonoCustomResourceRegistry.RegisteredType(nameof(Item), "", nameof(Resource))]
namespace Recursia;
public partial class Item : Resource, ISerializable
{
    public static readonly Item Empty = new("empty", "empty") {MaxStack = 0};
    [Export] public string DisplayName;
    [Export] public int MaxStack = 999;
    [Export] public float Cooldown = 0;
    [Export] public Texture2D? Texture2D;
    [Export] public AudioStream? UseSound;

    public string TypeName;

    public Item(string typeName, string displayname)
    {
        TypeName = typeName;
        DisplayName = displayname;
    }

    public virtual void OnUse(Combatant user, Vector3 position, Vector3 dir, ref ItemStack source)
    {
        user.ItemCooldown = Cooldown;
    }

    public virtual void Serialize(BinaryWriter bw) {}
    public virtual void Deserialize(BinaryReader br){}

    //allows subclasses to override .Equals, keeping == and != consistent with that
    public static bool operator ==(Item? a, Item? b)
    {
        return a is null ? b is null : a.Equals(b);
    }
    public static bool operator !=(Item? a, Item? b) => !(a==b);
}