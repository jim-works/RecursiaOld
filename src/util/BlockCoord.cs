
//Faces of blocks are on integral coordinates
//Ex: Block at (0,0,0) has corners (0,0,0) and (1,1,1)
public struct BlockCoord
{
    public int x, y,z;
    public BlockCoord(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    public int sqrMag() { return x * x + y * y + z*z; }
    public static BlockCoord operator +(BlockCoord lhs, BlockCoord rhs)
    {
        return new BlockCoord(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z+rhs.z);
    }
    public static BlockCoord operator -(BlockCoord lhs, BlockCoord rhs)
    {
        return new BlockCoord(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z-rhs.z);
    }
    public static BlockCoord operator *(int lhs, BlockCoord rhs)
    {
        return new BlockCoord(lhs * rhs.x, lhs * rhs.y, lhs*rhs.z);
    }
    //rounds down, opposed to normal integer division which rounds towards 0
    public static BlockCoord operator /(BlockCoord lhs, int rhs)
    {
        return new BlockCoord(Divide.ToNegative(lhs.x,rhs), Divide.ToNegative(lhs.y,rhs), Divide.ToNegative(lhs.z, rhs));
    }
    //returns positive modulus of each coordinate (ex: (-1,0,0)%16=(15,0,0))
    public static BlockCoord operator %(BlockCoord lhs, int rhs)
    {
        BlockCoord coords = new BlockCoord(lhs.x%rhs,lhs.y%rhs,lhs.z%rhs);
        coords.x = coords.x < 0 ? coords.x + rhs : coords.x;
        coords.y = coords.y < 0 ? coords.y + rhs : coords.y;
        coords.z = coords.z < 0 ? coords.z + rhs : coords.z;
        return coords;
    }
    public static bool operator ==(BlockCoord lhs, BlockCoord rhs)
    {
        return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;
    }
    public static bool operator !=(BlockCoord lhs, BlockCoord rhs)
    {
        return lhs.x != rhs.x || lhs.y != rhs.y || lhs.z != rhs.z;
    }
    //rounds each coordinate down, opposed to normal int casting which rounds towards 0
    public static explicit operator BlockCoord(Godot.Vector3 conv)
    {
        return new BlockCoord(Godot.Mathf.FloorToInt(conv.x), Godot.Mathf.FloorToInt(conv.y), Godot.Mathf.FloorToInt(conv.z));
    }
    public static explicit operator Godot.Vector3(BlockCoord conv)
    {
        return new Godot.Vector3(conv.x, conv.y, conv.z);
    }
    public static explicit operator BlockCoord(ChunkCoord conv)
    {
        return new BlockCoord(conv.x*Chunk.CHUNK_SIZE,conv.y*Chunk.CHUNK_SIZE,conv.z*Chunk.CHUNK_SIZE);
    }
    public override string ToString()
    {
        return $"({x}, {y}, {z})";
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
            return x.GetHashCode() * 31 + y.GetHashCode() * 17 + z.GetHashCode()*11;
        }
    }
}