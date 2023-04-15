using Godot;
using System.Collections.Generic;

namespace Recursia;
public partial class ChainStriker : Combatant
{
    [Export]
    public PackedScene? ChainLink;
    [Export]
    public int ChainSize = 5;
    [Export]
    public float StrikeImpulse = 5;
    [Export]
    public double AttackInterval = 3;
    [Export] public float AggroRange = 250;

    private double attackTimer;
    private readonly List<Combatant> links = new();

    public override void _Ready()
    {
        Combatant prev = this;
        links.Add(this);
        for (int i = 0; i < ChainSize; i++)
        {
            if (spawnLink(prev, 100+5*i) is Combatant c) {
                prev = c;
            }
        }
        base._Ready();
    }

    public override void _Process(double delta)
    {
        attackTimer += delta;
        if (attackTimer >= AttackInterval)
        {
            attack();
        }
        base._Process(delta);
    }
    public override void Die()
    {
        foreach (Combatant combatant in links)
        {
            if (combatant == this) continue;
            combatant.Die();
        }
        SphereShaper.Shape3D(World!, GlobalPosition, 6.2f);
        base.Die();
    }

    private void attack()
    {
        attackTimer = 0;
        if (World?.Entities.ClosestEnemy(GlobalPosition, Team, AggroRange, out Combatant? closest) ?? false)
        {
            if (closest == null) return;
            if (GlobalPosition.Y < closest.GlobalPosition.Y) AddImpulse(new Vector3(0, 10, 0)); //little hop
            AddImpulse((closest.GlobalPosition - GlobalPosition).Normalized() * StrikeImpulse);
        }
    }

    private Combatant? spawnLink(Combatant prev, float maxSpeed)
    {
        if (World?.Entities.SpawnObject<ChainLink>(ChainLink!, GlobalPosition, link => link.Parent = prev) is not ChainLink link) return null;
        link.MaxSpeed = maxSpeed;
        links.Add(link);
        return link;
    }
}