using Godot;

public partial class SegmentedCombatantChild : Combatant
{
    [Export] public NodePath ParentPath;
    public Combatant Parent;

    public override void _Ready()
    {
        if (ParentPath != null) Parent = GetNode<Combatant>(ParentPath);
        _physicsActive = Parent.PhysicsActive;
        Team = Parent.Team;
        base._Ready();
    }

    public override void TakeDamage(Damage damage)
    {
        Parent.TakeDamage(damage);
    }
    public override void Heal(float amount)
    {
        Parent.Heal(amount);
    }
    public override float GetHealth()
    {
        return Parent.GetHealth();
    }
    public override float GetMaxHealth()
    {
        return Parent.GetMaxHealth();
    }
}