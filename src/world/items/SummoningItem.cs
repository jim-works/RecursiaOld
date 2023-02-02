using Godot;

public class SummoningItem : Item
{
    public PackedScene ToSummon;
    public bool SetToUserTeam = false;
    public bool ConsumeOnUse = true;
    public float Distance;
    //will check at most this many blocks up for an open space to summon
    public int MaxCheckHeight = 50;

    public override void OnUse(Combatant user, Vector3 position, Vector3 dir, ref ItemStack source)
    {
        Combatant c = ToSummon.Instance<Combatant>();
        //summon randomly in a circle distance away from position
        float angle = GD.Randf()*2*Mathf.Pi;
        Vector3 offset = new Vector3(Mathf.Cos(angle)*Distance,0,Mathf.Sin(angle)*Distance);
        //find open space
        BlockCoord summonPos = (BlockCoord)(position+offset);
        int summonY = summonPos.y;
        for (int y = summonPos.y; y < summonPos.y+MaxCheckHeight; y++)
        {
            summonY=y;
            if (World.Singleton.GetBlock(new BlockCoord(summonPos.x,y,summonPos.z)) == null) break; //open space found
        }
        World.Singleton.AddChild(c);
        c.Position = new Vector3(position.x+offset.x,summonY,position.z+offset.z);
        if (SetToUserTeam) c.Team = user.Team;

        if (ConsumeOnUse) source.Size--;

        base.OnUse(user, position, dir, ref source);
    }
}