namespace Recursia;
public partial class Team
{
    public string TeamName;

    public Team()
    {
    }

    public Team(Team copy)
    {
        TeamName = copy.TeamName;
    }

    public static bool operator ==(Team a, Team b)
    {
        return (a is null && b is null) || a is not null && b is not null && a.TeamName == b.TeamName;
    }
    public static bool operator !=(Team a, Team b)
    {
        return !(a == b);
    }
    public override int GetHashCode()
    {
        return TeamName.GetHashCode();
    }
    public override bool Equals(object obj)
    {
        return (obj != null) && (obj is Team team) && team == this;
    }
    public override string ToString()
    {
        return TeamName;
    }
}