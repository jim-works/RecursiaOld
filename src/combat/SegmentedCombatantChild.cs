using Godot;

namespace Recursia;
public partial class SegmentedCombatantChild : Combatant
{
    [Export] public NodePath? ParentPath;
    public Combatant? Parent;

    public bool NoSerialize = true;

    public override void _EnterTree()
    {
        //get world pointer from Parent
        //this way we can create children in the editor, without having to find a way to call EntityCollection.SpawnObject for all of them
        if (ParentPath != null) Parent = GetNode<Combatant>(ParentPath);
        if (Parent == null)
        {
            GD.PushError($"Segmented combatant child has null parent! {ParentPath}");
            base._Ready();
            return;
        }
        World = Parent.World;
        base._EnterTree();
    }

    public override void _Ready()
    {
        if (Parent != null)
        {
            PhysicsActive = Parent.PhysicsActive;
            Team = Parent.Team;
        }
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