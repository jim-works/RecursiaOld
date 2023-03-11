using System.IO;

public struct ChunkGroupCoord
{
    public int X, Y,Z;
    public ChunkGroupCoord(int X, int Y, int Z)
    {
        this.X = X;
        this.Y = Y;
        this.Z = Z;
    }
    public int sqrMag() { return X * X + Y * Y + Z*Z; }
    public void Serialize(BinaryWriter bw)
    {
        bw.Write(X);
        bw.Write(Y);
        bw.Write(Z);
    }
    public static ChunkGroupCoord Deserialize(BinaryReader br)
    {
        return new ChunkGroupCoord(br.ReadInt32(),br.ReadInt32(),br.ReadInt32());
    }
    public static ChunkGroupCoord operator +(ChunkGroupCoord lhs, ChunkGroupCoord rhs)
    {
        return new ChunkGroupCoord(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z+rhs.Z);
    }
    public static ChunkGroupCoord operator -(ChunkGroupCoord lhs, ChunkGroupCoord rhs)
    {
        return new ChunkGroupCoord(lhs.X - rhs.X, lhs.Y - rhs.Y, lhs.Z-rhs.Z);
    }
    public static ChunkGroupCoord operator *(int lhs, ChunkGroupCoord rhs)
    {
        return new ChunkGroupCoord(lhs * rhs.X, lhs * rhs.Y, lhs*rhs.Z);
    }
    //rounds down, opposed to normal integer division which rounds towards 0
    public static ChunkGroupCoord operator /(ChunkGroupCoord lhs, int rhs)
    {
        return new ChunkGroupCoord(Divide.ToNegative(lhs.X,rhs), Divide.ToNegative(lhs.Y,rhs), Divide.ToNegative(lhs.Z, rhs));
    }
    //returns positive modulus of each coordinate (ex: (-1,0,0)%16=(15,0,0))
    public static ChunkGroupCoord operator %(ChunkGroupCoord lhs, int rhs)
    {
        ChunkGroupCoord coords = new ChunkGroupCoord(lhs.X%rhs,lhs.Y%rhs,lhs.Z%rhs);
        coords.X = coords.X < 0 ? coords.X + rhs : coords.X;
        coords.Y = coords.Y < 0 ? coords.Y + rhs : coords.Y;
        coords.Z = coords.Z < 0 ? coords.Z + rhs : coords.Z;
        return coords;
    }
    public static bool operator ==(ChunkGroupCoord lhs, ChunkGroupCoord rhs)
    {
        return lhs.X == rhs.X && lhs.Y == rhs.Y && lhs.Z == rhs.Z;
    }
    public static bool operator !=(ChunkGroupCoord lhs, ChunkGroupCoord rhs)
    {
        return lhs.X != rhs.X || lhs.Y != rhs.Y || lhs.Z != rhs.Z;
    }
    //rounds each coordinate down, opposed to normal int casting which rounds towards 0
    public static explicit operator ChunkGroupCoord(ChunkCoord conv)
    {
        ChunkCoord c = conv/ChunkGroup.GROUP_SIZE;
        return new ChunkGroupCoord(c.X,c.Y,c.Z);
    }
    public override string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }
    public override bool Equals(object obj)
    {
        switch (obj)
        {
            case ChunkGroupCoord ChunkGroupCoord: return ChunkGroupCoord == this;
            default: return false;
        }
    }
    public override int GetHashCode()
    {
        unchecked
        {
            return X.GetHashCode() * 31 + Y.GetHashCode() * 17 + Z.GetHashCode()*11;
        }
    }
}