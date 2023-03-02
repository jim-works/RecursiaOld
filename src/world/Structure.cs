public partial class Structure
{
    public string Name;
    public BlockCoord Position;
    public int Priority;
    public bool Mutex; //don't allow other mutex structures to be generated in this structure's radius
    public BlockCoord Size;
}