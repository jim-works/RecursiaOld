using Godot;
using System.Collections.Generic;

public class ChainStriker : Combatant
{
    [Export]
    public PackedScene ChainLink;
    [Export]
    public int ChainSize = 5;
    [Export]
    public float StrikeImpulse = 5;
    [Export]
    public float AttackInterval = 3;

    private float attackTimer = 0;
    private List<PhysicsObject> links = new List<PhysicsObject>();

    public override void _Ready()
    {
        PhysicsObject prev = this;
        links.Add(this);
        for (int i = 0; i < ChainSize; i++)
        {
            prev = spawnLink(prev, 100+5*i);
        }
        base._Ready();
    }

    public override void _Process(float delta)
    {
        attackTimer += delta;
        if (attackTimer >= AttackInterval)
        {
            attack();
        }
        base._Process(delta);
    }

    private void attack()
    {
        attackTimer = 0;
        Player closest = World.Singleton.ClosestPlayer(Position);
        // for (int i = 0; i < links.Count; i++)
        // {
        //     var link = links[i];
        //     if (link.Position.y < closest.Position.y) link.AddImpulse(new Vector3(0,10,0)); //little hop
        //     link.AddImpulse((closest.Position-link.Position).Normalized()*StrikeImpulse);
        // }\
        if (Position.y < closest.Position.y) AddImpulse(new Vector3(0,10,0)); //little hop
        AddImpulse((closest.Position-Position).Normalized()*StrikeImpulse);
    }

    private PhysicsObject spawnLink(PhysicsObject prev, float maxSpeed)
    {
        ChainLink link = ChainLink.Instance<ChainLink>();
        link.AttachedTo = prev;
        link.MaxSpeed = maxSpeed;
        links.Add(link);

        GetParent().CallDeferred("add_child", link); //on the same level as the link
        return link;
    }
}