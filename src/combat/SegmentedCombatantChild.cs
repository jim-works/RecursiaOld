using System.Collections.Generic;

public class SegmentedCombatantChild : Combatant
{
    public Combatant Parent;


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