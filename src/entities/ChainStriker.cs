using Godot;
using System.Linq;

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

    public override void _Ready()
    {
        PhysicsObject prev = this;
        for (int i = 0; i < ChainSize; i++)
        {
            prev = spawnLink(prev);
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
        PhysicsObject curr = this;
        while (curr != null)
        {
            //fling each link toward the player with impulse proportional to how far it is down the chain;
            if (curr.Position.y < closest.Position.y) curr.AddImpulse(new Vector3(0,10,0)); //little hop
            curr.AddImpulse((closest.Position-curr.Position).Normalized()*((curr.Position-this.Position).Length()+1)*StrikeImpulse);
            curr = curr.GetChildOrNull<PhysicsObject>(0);
        }
    }

    private PhysicsObject spawnLink(PhysicsObject prev)
    {
        ChainLink link = ChainLink.Instance<ChainLink>();
        link.Parent = prev;

        GetParent().CallDeferred("add_child", link); //on the same level as the link
        return link;
    }
}