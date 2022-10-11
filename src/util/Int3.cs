public struct Int3
{
    public int x, y,z;
    public Int3(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    public int sqrMag() { return x * x + y * y + z*z; }
    public static Int3 operator +(Int3 lhs, Int3 rhs)
    {
        return new Int3(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z+rhs.z);
    }
    public static Int3 operator -(Int3 lhs, Int3 rhs)
    {
        return new Int3(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z-rhs.z);
    }
    public static Int3 operator *(int lhs, Int3 rhs)
    {
        return new Int3(lhs * rhs.x, lhs * rhs.y, lhs*rhs.z);
    }
    public static Int3 operator /(Int3 lhs, int rhs)
    {
        return new Int3(Divide.ToNegative(lhs.x,rhs), Divide.ToNegative(lhs.y,rhs), Divide.ToNegative(lhs.z, rhs));
    }
    public static Int3 operator %(Int3 lhs, int rhs)
    {
        return new Int3(lhs.x % rhs, lhs.y % rhs, lhs.z % rhs);
    }
    public static bool operator ==(Int3 lhs, Int3 rhs)
    {
        return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;
    }
    public static bool operator !=(Int3 lhs, Int3 rhs)
    {
        return lhs.x != rhs.x || lhs.y != rhs.y || lhs.z != rhs.z;
    }
    public static explicit operator Int3(Godot.Vector3 conv)
    {
        return new Int3((int)conv.x, (int)conv.y, (int)conv.z);
    }
    public override string ToString()
    {
        return $"({x}, {y}, {z})";
    }
    public override bool Equals(object obj)
    {
        switch (obj)
        {
            case Int3 Int3: return Int3 == this;
            default: return false;
        }
    }
    public override int GetHashCode()
    {
        unchecked
        {
            return x.GetHashCode() * 31 + y.GetHashCode() * 17 + z.GetHashCode()*11;
        }
    }
}