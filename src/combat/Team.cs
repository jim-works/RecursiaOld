public struct Team
{
    public string TeamName;

    public static bool operator ==(Team a, Team b)
    {
        return (a==null&&b==null)||a!=null&&b!=null&&a.TeamName == b.TeamName;
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
}