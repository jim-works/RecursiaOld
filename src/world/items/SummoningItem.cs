using Godot;

namespace Recursia;
public partial class SummoningItem : Item
{
    [Export] public PackedScene? ToSummon;
    [Export] public bool SetToUserTeam;
    [Export] public bool ConsumeOnUse = true;
    [Export] public float Distance;
    //will check at most this many blocks up for an open space to summon
    [Export] public int MaxCheckHeight = 50;

    public SummoningItem(string typeName, string displayname) : base(typeName, displayname){}

    public override bool OnUse(Combatant user, Vector3 position, Vector3 dir, ref ItemStack source)
    {
        if (user.World == null) throw new System.FieldAccessException("User's world is null!");
        //summon randomly in a circle distance away from position
        float angle = GD.Randf()*2*Mathf.Pi;
        Vector3 offset = new(Mathf.Cos(angle)*Distance,0,Mathf.Sin(angle)*Distance);
        //find open space
        BlockCoord summonPos = (BlockCoord)(position+offset);
        int summonY = summonPos.Y;
        for (int y = summonPos.Y; y < summonPos.Y+MaxCheckHeight; y++)
        {
            summonY=y;
            if (user.World.Chunks.GetBlock(new BlockCoord(summonPos.X,y,summonPos.Z)) == null) break; //open space found
        }
        if (ToSummon == null)
        {
            GD.PushWarning("ToSummon is null!");
            base.OnUse(user, position, dir, ref source);
            return false;
        }
        Combatant c = user.World.Entities.SpawnObject<Combatant>(ToSummon, new Vector3(position.X+offset.X,summonY,position.Z+offset.Z));
        if (SetToUserTeam) c.Team = user.Team;
        if (ConsumeOnUse) source.Size--;
        base.OnUse(user, position, dir, ref source);
        return true;
    }
}