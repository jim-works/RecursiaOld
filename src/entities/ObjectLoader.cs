using Godot;

namespace Recursia;
public partial class ObjectLoader : Node
{
    [Export] public PackedScene[] ObjectScenes = null!;
    [Export] public string[] ObjectTypeNames = null!;

    public override void _EnterTree()
    {
        Load();
        base._EnterTree();
    }

    public void Load()
    {
        for (int i = 0; i < ObjectScenes.Length; i++)
        {
            var scene = ObjectScenes[i];
            var type = ObjectTypeNames[i];
            ObjectTypes.CreateType(type, scene);
        }
    }
}