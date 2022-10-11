public struct Int2
{
    public int x, y;
    public Int2(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
    public int sqrMag() { return x * x + y * y; }
    public static Int2 operator +(Int2 lhs, Int2 rhs)
    {
        return new Int2(lhs.x + rhs.x, lhs.y + rhs.y);
    }
    public static Int2 operator -(Int2 lhs, Int2 rhs)
    {
        return new Int2(lhs.x - rhs.x, lhs.y - rhs.y);
    }
    public static Int2 operator *(int lhs, Int2 rhs)
    {
        return new Int2(lhs * rhs.x, lhs * rhs.y);
    }
    public static Int2 operator /(Int2 lhs, int rhs)
    {
        return new Int2(Divide.ToNegative(lhs.x,rhs), Divide.ToNegative(lhs.y,rhs));
    }
    public static Int2 operator %(Int2 lhs, int rhs)
    {
        return new Int2(lhs.x % rhs, lhs.y % rhs);
    }
    public static bool operator ==(Int2 lhs, Int2 rhs)
    {
        return lhs.x == rhs.x && lhs.y == rhs.y;
    }
    public static bool operator !=(Int2 lhs, Int2 rhs)
    {
        return lhs.x != rhs.x || lhs.y != rhs.y;
    }
    public static explicit operator Int2(Godot.Vector2 conv)
    {
        return new Int2((int)conv.x, (int)conv.y);
    }
    public override string ToString()
    {
        return $"({x}, {y})";
    }
    public override bool Equals(object obj)
    {
        switch (obj)
        {
            case Int2 int2: return int2 == this;
            default: return false;
        }
    }
    public override int GetHashCode()
    {
        unchecked
        {
            return x.GetHashCode() * 31 + y.GetHashCode() * 17;
        }
    }
}