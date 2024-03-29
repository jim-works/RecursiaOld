using Godot;

public partial class Combatant : PhysicsObject
{
    private float health;
    private float maxHealth;
    public Team Team = null;
    [Export] public string InitialTeamName;
    [Export] public float InitialHealth;
    [Export] public double InvincibilitySeconds = 0.001f; //should be 1 frame
    [Export] public float ContactDamage = 1;

    [Export] public NodePath AudioPlayer = "AudioStreamPlayer3D";

    public Inventory Inventory;
    public double ItemCooldown;

    private double invicinibilityTimer = 0;
    private AudioStreamPlayer3D audioStreamPlayer;

    public override void _Ready()
    {
        base._Ready();
        World.Singleton.Combatants.Add(this);
        audioStreamPlayer = GetNodeOrNull<AudioStreamPlayer3D>(AudioPlayer);
        
        if (Team == null && !string.IsNullOrEmpty(InitialTeamName)) Team = new Team{TeamName=InitialTeamName};
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
        if (ContactDamage != 0)  DoContactDamage();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Block b = World.Singleton.GetBlock((BlockCoord)GlobalPosition+new BlockCoord(x,y,z));
                    if (b != null && b.Name == "lava")
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

    public override void _ExitTree()
    {
        World.Singleton.Combatants.Remove(this);
        base._ExitTree();
    }

    public void UseItem(int slot, Vector3 offset, Vector3 direction)
    {
        if (ItemCooldown > 0) return;
        Inventory.Items[slot].Item?.OnUse(this, offset, direction, ref Inventory.Items[slot]);
        //play usage sound
        PlaySound(Inventory.Items[slot].Item?.UseSound);
        Inventory.TriggerUpdate();
    }

    public void PlaySound(AudioStream clip)
    {
        if (audioStreamPlayer != null && clip != null)
        {
            GD.Print("played sound on " + Name);
            audioStreamPlayer.Stream = clip;
            audioStreamPlayer.Play();
        }
    }

    public virtual void DoContactDamage()
    {
        foreach (var combatant in World.Singleton.Combatants)
        {
            if (combatant == this) continue;
            if (combatant.GetBox().IntersectsBox(GetBox())) combatant.TakeDamage(new Damage{Team=Team,Amount=ContactDamage});
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
}