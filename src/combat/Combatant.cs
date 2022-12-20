using Godot;

public class Combatant : PhysicsObject
{
    private float health;
    private float maxHealth;
    public Team Team {get; protected set;}
    [Export]
    public string InitialTeamName;
    [Export]
    public float InitialHealth;

    public override void _EnterTree()
    {
        base._EnterTree();
        World.Singleton.Combatants.Add(this);
        if (!string.IsNullOrEmpty(InitialTeamName)) Team = new Team{TeamName=InitialTeamName};
        if (InitialHealth > 0) {
            maxHealth = InitialHealth;
            health = InitialHealth;
        }
    }

    public override void _ExitTree()
    {
        World.Singleton.Combatants.Remove(this);
        base._ExitTree();
    }

    public virtual void TakeDamage(Damage damage)
    {
        if (damage.Team == Team) return; //no friendly fire
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