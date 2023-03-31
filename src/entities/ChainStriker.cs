using Godot;
using System.Collections.Generic;

public partial class ChainStriker : Combatant
{
    [Export]
    public PackedScene ChainLink;
    [Export]
    public int ChainSize = 5;
    [Export]
    public float StrikeImpulse = 5;
    [Export]
    public double AttackInterval = 3;
    [Export] public float AggroRange = 250;

    private double attackTimer = 0;
    private List<Combatant> links = new List<Combatant>();

    public override void _Ready()
    {
        Combatant prev = this;
        links.Add(this);
        for (int i = 0; i < ChainSize; i++)
        {
            prev = spawnLink(prev, 100+5*i);
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
        SphereShaper.Shape3D(World.Singleton, GlobalPosition, 6.2f);
        base.Die();
    }

    private void attack()
    {
        attackTimer = 0;
        if (World.Singleton.ClosestEnemy(GlobalPosition, Team, AggroRange, out Combatant closest))
        {
            if (GlobalPosition.Y < closest.GlobalPosition.Y) AddImpulse(new Vector3(0, 10, 0)); //little hop
            AddImpulse((closest.GlobalPosition - GlobalPosition).Normalized() * StrikeImpulse);
        }
    }

    private Combatant spawnLink(Combatant prev, float maxSpeed)
    {
        ChainLink link = World.Singleton.SpawnObject<ChainLink>(ChainLink, GlobalPosition, link => link.Parent = prev);
        link.MaxSpeed = maxSpeed;
        links.Add(link);
        return link;
    }
}