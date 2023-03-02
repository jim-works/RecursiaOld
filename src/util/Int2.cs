public struct Int2
{
    public int X, Y;
    public Int2(int X, int Y)
    {
        this.X = X;
        this.Y = Y;
    }
    public int sqrMag() { return X * X + Y * Y; }
    public static Int2 operator +(Int2 lhs, Int2 rhs)
    {
        return new Int2(lhs.X + rhs.X, lhs.Y + rhs.Y);
    }
    public static Int2 operator -(Int2 lhs, Int2 rhs)
    {
        return new Int2(lhs.X - rhs.X, lhs.Y - rhs.Y);
    }
    public static Int2 operator *(int lhs, Int2 rhs)
    {
        return new Int2(lhs * rhs.X, lhs * rhs.Y);
    }
    public static Int2 operator /(Int2 lhs, int rhs)
    {
        return new Int2(Divide.ToNegative(lhs.X,rhs), Divide.ToNegative(lhs.Y,rhs));
    }
    public static Int2 operator %(Int2 lhs, int rhs)
    {
        return new Int2(lhs.X % rhs, lhs.Y % rhs);
    }
    public static bool operator ==(Int2 lhs, Int2 rhs)
    {
        return lhs.X == rhs.X && lhs.Y == rhs.Y;
    }
    public static bool operator !=(Int2 lhs, Int2 rhs)
    {
        return lhs.X != rhs.X || lhs.Y != rhs.Y;
    }
    public static explicit operator Int2(Godot.Vector2 conv)
    {
        return new Int2((int)conv.X, (int)conv.Y);
    }
    public override string ToString()
    {
        return $"({X}, {Y})";
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
            return X.GetHashCode() * 31 + Y.GetHashCode() * 17;
        }
    }
}