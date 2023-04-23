using System.IO;

namespace Recursia;
public class Team : ISerializable
{
    public string TeamName;

    public Team(string name)
    {
        TeamName = name;
    }

    public Team(Team copy)
    {
        TeamName = copy.TeamName;
    }

    public static bool operator ==(Team? a, Team? b)
    {
        return (a is null && b is null) || a is not null && b is not null && a.TeamName == b.TeamName;
    }
    public static bool operator !=(Team? a, Team? b)
    {
        return !(a == b);
    }
    public override int GetHashCode()
    {
        return TeamName.GetHashCode();
    }
    public override bool Equals(object? obj)
    {
        return (obj is Team team) && team == this;
    }
    public override string ToString()
    {
        return TeamName;
    }
    public void Serialize(BinaryWriter bw)
    {
        bw.Write(TeamName);
    }
    public void Deserialize(BinaryReader br)
    {
        TeamName = br.ReadString();
    }
}