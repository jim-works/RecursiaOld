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
        
        return (object.ReferenceEquals(a,null)&&object.ReferenceEquals(b,null))||!object.ReferenceEquals(a,null)&&!object.ReferenceEquals(b,null)&&a.TeamName == b.TeamName;
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
        return (obj != null) && (obj is Team) && (Team)obj == this;
    }
    public override string ToString()
    {
        return TeamName;
    }
}