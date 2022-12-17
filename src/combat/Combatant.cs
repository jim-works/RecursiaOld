using Godot;

public class Combatant : PhysicsObject
{
    public float Health {get; protected set;}
    public float MaxHealth {get; protected set;}
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
            MaxHealth = InitialHealth;
            Health = InitialHealth;
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
        Health = Mathf.Max(0,Health-damage.Amount);
        GD.Print($"{Name} took {damage.Amount} damage. Health={Health}");
        if (Health <= 0) Die();
    } 
    public virtual void Die()
    {
        QueueFree();
    }
    public virtual void Heal(float amount)
    {
        Health = Mathf.Min(MaxHealth, Health+amount);
    }
}