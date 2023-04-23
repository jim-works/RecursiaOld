using System.IO;
using Godot;

namespace Recursia;
public partial class Combatant : PhysicsObject
{
    private float health;
    private float maxHealth;
    public Team? Team;
    [Export] public string? InitialTeamName;
    [Export] public float InitialHealth;
    [Export] public double InvincibilitySeconds = 0.001f; //should be 1 frame

    [Export] public NodePath AudioPlayer = "AudioStreamPlayer3D";

    public Inventory? Inventory;
    public double ItemCooldown;

    private double invicinibilityTimer;
    private AudioStreamPlayer3D? audioStreamPlayer;

    public override void _Ready()
    {
        base._Ready();
        audioStreamPlayer = GetNodeOrNull<AudioStreamPlayer3D>(AudioPlayer);

        if (Team == null && !string.IsNullOrEmpty(InitialTeamName)) Team = new Team(InitialTeamName);
        if (InitialHealth > 0) {
            maxHealth = InitialHealth;
            health = InitialHealth;
        }
        GD.Print($"Added combatant {Name} on team {Team}");
    }

    public override void _Process(double delta)
    {
        invicinibilityTimer += delta;
        ItemCooldown -= delta;
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Block? b = World?.Chunks.GetBlock((BlockCoord)GlobalPosition+new BlockCoord(x,y,z));
                    if (b?.Name == "lava")
                    {
                        TakeDamage(new Damage{
                            Amount=1
                        });
                    }
                }
            }
        }
        base._Process(delta);
    }

    public void UseItem(int slot, Vector3 offset, Vector3 direction)
    {
        if (ItemCooldown > 0 || Inventory == null) return;
        if (Inventory.Items[slot].Item.OnUse(this, offset, direction, ref Inventory.Items[slot])) PlaySound(Inventory.Items[slot].Item.UseSound);
        Inventory.TriggerUpdate();
    }

    public void PlaySound(AudioStream? clip)
    {
        if (audioStreamPlayer != null && clip != null)
        {
            GD.Print("played sound on " + Name);
            audioStreamPlayer.Stream = clip;
            audioStreamPlayer.Play();
        }
    }

    public virtual void TakeDamage(Damage damage)
    {
        if (damage.Team == Team || invicinibilityTimer < InvincibilitySeconds) return; //no friendly fire, do iframes
        invicinibilityTimer = 0;
        health = Mathf.Max(0,health-damage.Amount);
        GD.Print($"{Name} took {damage.Amount} damage. Health={health}");
        if (health <= 0) Die();
    }
    public virtual void Die()
    {
        QueueFree();
    }
    public virtual void Heal(float amount)
    {
        health = Mathf.Min(maxHealth, health+amount);
    }
    public virtual float GetMaxHealth()
    {
        return maxHealth;
    }
    public virtual float GetHealth()
    {
        return health;
    }
    public override void Serialize(BinaryWriter bw)
    {
        base.Serialize(bw);
        bw.Write(maxHealth);
        bw.Write(health);
        SerializationExtensions.SerializeMaybeNull(Team, bw);
        SerializationExtensions.SerializeMaybeNull(Inventory, bw);
        bw.Write(ItemCooldown);
        bw.Write(invicinibilityTimer);
    }
    public override void Deserialize(BinaryReader br)
    {
        base.Deserialize(br);
        maxHealth = br.ReadSingle();
        health = br.ReadSingle();
        Team tmp = new("");
        Team = SerializationExtensions.DeserializeMaybeNull(tmp, br) ? tmp : null;
        Inventory = new(0);
        Inventory = SerializationExtensions.DeserializeMaybeNull(Inventory, br) ? Inventory : null;
        ItemCooldown = br.ReadDouble();
        invicinibilityTimer = br.ReadDouble();

        InitialHealth = -1; //so that the loaded value isn't overwritten on _ready
        InitialTeamName = null; //same here
    }
}