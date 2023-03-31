using Godot;

public partial class ObjectLoader : Node
{
    [Export] public PackedScene[] ObjectScenes;
    [Export] public string[] ObjectTypeNames;

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