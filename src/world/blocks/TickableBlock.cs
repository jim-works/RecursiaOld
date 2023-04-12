//not sure exactly how to do this yet
namespace Recursia;
public abstract class TickableBlock : Block
{
    public override void OnLoad(BlockCoord pos, Chunk c)
    {}

    public override void OnUnload(BlockCoord pos, Chunk c)
    {}

    public abstract void OnTick(BlockCoord coord, float dt);
}