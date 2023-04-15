using Godot;

namespace Recursia;
public partial class SegmentedCombatantChild : Combatant
{
    [Export] public NodePath? ParentPath;
    public Combatant? Parent;

    public override void _Ready()
    {
        if (ParentPath != null) Parent = GetNode<Combatant>(ParentPath);
        if (Parent == null)
        {
            GD.PushError($"Segmented combatant child has null parent! {ParentPath}");
            base._Ready();
            return;
        }
        PhysicsActive = Parent.PhysicsActive;
        Team = Parent.Team;
        base._Ready();
    }

    public override void TakeDamage(Damage damage)
    {
        Parent?.TakeDamage(damage);
    }
    public override void Heal(float amount)
    {
        Parent?.Heal(amount);
    }
    public override float GetHealth()
    {
        return Parent?.GetHealth() ?? 0;
    }
    public override float GetMaxHealth()
    {
        return Parent?.GetMaxHealth() ?? 1;
    }
}