using System.IO;

namespace Recursia;
public struct ChunkCoord
{
    public int X, Y,Z;
    public ChunkCoord(int X, int Y, int Z)
    {
        this.X = X;
        this.Y = Y;
        this.Z = Z;
    }
    public int SqrMag() { return X * X + Y * Y + Z*Z; }
    public void Serialize(BinaryWriter bw)
    {
        bw.Write(X);
        bw.Write(Y);
        bw.Write(Z);
    }
    public static ChunkCoord Deserialize(BinaryReader br)
    {
        return new ChunkCoord(br.ReadInt32(),br.ReadInt32(),br.ReadInt32());
    }
    public static ChunkCoord operator +(ChunkCoord lhs, ChunkCoord rhs)
    {
        return new ChunkCoord(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z+rhs.Z);
    }
    public static ChunkCoord operator -(ChunkCoord lhs, ChunkCoord rhs)
    {
        return new ChunkCoord(lhs.X - rhs.X, lhs.Y - rhs.Y, lhs.Z-rhs.Z);
    }
    public static ChunkCoord operator *(int lhs, ChunkCoord rhs)
    {
        return new ChunkCoord(lhs * rhs.X, lhs * rhs.Y, lhs*rhs.Z);
    }
    //rounds down, opposed to normal integer division which rounds towards 0
    public static ChunkCoord operator /(ChunkCoord lhs, int rhs)
    {
        return new ChunkCoord(Divide.ToNegative(lhs.X,rhs), Divide.ToNegative(lhs.Y,rhs), Divide.ToNegative(lhs.Z, rhs));
    }
    //returns positive modulus of each coordinate (ex: (-1,0,0)%16=(15,0,0))
    public static ChunkCoord operator %(ChunkCoord lhs, int rhs)
    {
        ChunkCoord coords = new(lhs.X%rhs,lhs.Y%rhs,lhs.Z%rhs);
        coords.X = coords.X < 0 ? coords.X + rhs : coords.X;
        coords.Y = coords.Y < 0 ? coords.Y + rhs : coords.Y;
        coords.Z = coords.Z < 0 ? coords.Z + rhs : coords.Z;
        return coords;
    }
    public static bool operator ==(ChunkCoord lhs, ChunkCoord rhs)
    {
        return lhs.X == rhs.X && lhs.Y == rhs.Y && lhs.Z == rhs.Z;
    }
    public static bool operator !=(ChunkCoord lhs, ChunkCoord rhs)
    {
        return lhs.X != rhs.X || lhs.Y != rhs.Y || lhs.Z != rhs.Z;
    }
    //rounds each coordinate down, opposed to normal int casting which rounds towards 0
    public static explicit operator ChunkCoord(BlockCoord conv)
    {
        BlockCoord c = conv/Chunk.CHUNK_SIZE;
        return new ChunkCoord(c.X,c.Y,c.Z);
    }
    public static explicit operator ChunkCoord(Godot.Vector3 conv)
    {
        BlockCoord c = (BlockCoord)conv/Chunk.CHUNK_SIZE;
        return new ChunkCoord(c.X,c.Y,c.Z);
    }
    public override string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }
    public override bool Equals(object obj)
    {
        return obj switch
        {
            ChunkCoord ChunkCoord => ChunkCoord == this,
            _ => false,
        };
    }
    public override int GetHashCode()
    {
        unchecked
        {
            return X.GetHashCode() * 31 + Y.GetHashCode() * 17 + Z.GetHashCode()*11;
        }
    }
}