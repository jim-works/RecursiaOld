public struct ChunkCoord
{
    public int x, y,z;
    public ChunkCoord(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    public int sqrMag() { return x * x + y * y + z*z; }
    public static ChunkCoord operator +(ChunkCoord lhs, ChunkCoord rhs)
    {
        return new ChunkCoord(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z+rhs.z);
    }
    public static ChunkCoord operator -(ChunkCoord lhs, ChunkCoord rhs)
    {
        return new ChunkCoord(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z-rhs.z);
    }
    public static ChunkCoord operator *(int lhs, ChunkCoord rhs)
    {
        return new ChunkCoord(lhs * rhs.x, lhs * rhs.y, lhs*rhs.z);
    }
    //rounds down, opposed to normal integer division which rounds towards 0
    public static ChunkCoord operator /(ChunkCoord lhs, int rhs)
    {
        return new ChunkCoord(Divide.ToNegative(lhs.x,rhs), Divide.ToNegative(lhs.y,rhs), Divide.ToNegative(lhs.z, rhs));
    }
    //returns positive modulus of each coordinate (ex: (-1,0,0)%16=(15,0,0))
    public static ChunkCoord operator %(ChunkCoord lhs, int rhs)
    {
        ChunkCoord coords = new ChunkCoord(lhs.x%rhs,lhs.y%rhs,lhs.z%rhs);
        coords.x = coords.x < 0 ? coords.x + rhs : coords.x;
        coords.y = coords.y < 0 ? coords.y + rhs : coords.y;
        coords.z = coords.z < 0 ? coords.z + rhs : coords.z;
        return coords;
    }
    public static bool operator ==(ChunkCoord lhs, ChunkCoord rhs)
    {
        return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;
    }
    public static bool operator !=(ChunkCoord lhs, ChunkCoord rhs)
    {
        return lhs.x != rhs.x || lhs.y != rhs.y || lhs.z != rhs.z;
    }
    //rounds each coordinate down, opposed to normal int casting which rounds towards 0
    public static explicit operator ChunkCoord(BlockCoord conv)
    {
        BlockCoord c = conv/Chunk.CHUNK_SIZE;
        return new ChunkCoord(c.x,c.y,c.z);
    }
    public override string ToString()
    {
        return $"({x}, {y}, {z})";
    }
    public override bool Equals(object obj)
    {
        switch (obj)
        {
            case ChunkCoord ChunkCoord: return ChunkCoord == this;
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