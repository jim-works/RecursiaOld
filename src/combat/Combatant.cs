using Godot;

public class Combatant : PhysicsObject
{
    private float health;
    private float maxHealth;
    public Team Team = null;
    [Export]
    public string InitialTeamName;
    [Export]
    public float InitialHealth;
    [Export]
    public float InvincibilitySeconds = 0.001f; //should be 1 frame
    [Export]
    public float ContactDamage = 1;

    public Inventory Inventory;
    public float ItemCooldown;

    private float invicinibilityTimer = 0;

    public override void _Ready()
    {
        base._Ready();
        World.Singleton.Combatants.Add(this);
        
        if (Team == null && !string.IsNullOrEmpty(InitialTeamName)) Team = new Team{TeamName=InitialTeamName};
        if (InitialHealth > 0) {
            maxHealth = InitialHealth;
            health = InitialHealth;
        }
        GD.Print($"Added combatant {Name} on team {Team}");
    }

    public override void _Process(float delta)
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
                    Block b = World.Singleton.GetBlock((BlockCoord)Position+new BlockCoord(x,y,z));
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
        Inventory.TriggerUpdate();
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