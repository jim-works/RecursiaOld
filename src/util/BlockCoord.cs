using System.IO;

//Faces of blocks are on integral coordinates
//EX: Block at (0,0,0) has corners (0,0,0) and (1,1,1)
public struct BlockCoord : ISerializable
{
    public int X, Y,Z;
    public BlockCoord(int X, int Y, int Z)
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
    public static BlockCoord Deserialize(BinaryReader br)
    {
        return new BlockCoord(br.ReadInt32(),br.ReadInt32(),br.ReadInt32());
    }
    public static BlockCoord operator +(BlockCoord lhs, BlockCoord rhs)
    {
        return new BlockCoord(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z+rhs.Z);
    }
    public static BlockCoord operator -(BlockCoord lhs, BlockCoord rhs)
    {
        return new BlockCoord(lhs.X - rhs.X, lhs.Y - rhs.Y, lhs.Z-rhs.Z);
    }
    public static BlockCoord operator *(int lhs, BlockCoord rhs)
    {
        return new BlockCoord(lhs * rhs.X, lhs * rhs.Y, lhs*rhs.Z);
    }
    //rounds down, opposed to normal integer division which rounds towards 0
    public static BlockCoord operator /(BlockCoord lhs, int rhs)
    {
        return new BlockCoord(Divide.ToNegative(lhs.X,rhs), Divide.ToNegative(lhs.Y,rhs), Divide.ToNegative(lhs.Z, rhs));
    }
    //returns positive modulus of each coordinate (eX: (-1,0,0)%16=(15,0,0))
    public static BlockCoord operator %(BlockCoord lhs, int rhs)
    {
        BlockCoord coords = new BlockCoord(lhs.X%rhs,lhs.Y%rhs,lhs.Z%rhs);
        coords.X = coords.X < 0 ? coords.X + rhs : coords.X;
        coords.Y = coords.Y < 0 ? coords.Y + rhs : coords.Y;
        coords.Z = coords.Z < 0 ? coords.Z + rhs : coords.Z;
        return coords;
    }
    public static bool operator ==(BlockCoord lhs, BlockCoord rhs)
    {
        return lhs.X == rhs.X && lhs.Y == rhs.Y && lhs.Z == rhs.Z;
    }
    public static bool operator !=(BlockCoord lhs, BlockCoord rhs)
    {
        return lhs.X != rhs.X || lhs.Y != rhs.Y || lhs.Z != rhs.Z;
    }
    //rounds each coordinate down, opposed to normal int casting which rounds towards 0
    public static explicit operator BlockCoord(Godot.Vector3 conv)
    {
        return new BlockCoord(Godot.Mathf.FloorToInt(conv.X), Godot.Mathf.FloorToInt(conv.Y), Godot.Mathf.FloorToInt(conv.Z));
    }
    public static explicit operator Godot.Vector3(BlockCoord conv)
    {
        return new Godot.Vector3(conv.X, conv.Y, conv.Z);
    }
    public static explicit operator BlockCoord(ChunkCoord conv)
    {
        return new BlockCoord(conv.X*(int)Chunk.CHUNK_SIZE,conv.Y*(int)Chunk.CHUNK_SIZE,conv.Z*(int)Chunk.CHUNK_SIZE);
    }
    public override string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }
    public override bool Equals(object obj)
    {
        switch (obj)
        {
            case BlockCoord BlockCoord: return BlockCoord == this;
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