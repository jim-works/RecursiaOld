using Godot;

public class ItemLoader : Node
{
    [Export]
    public Texture GunTexture;

    public override void _EnterTree()
    {
        Load();
        base._EnterTree();
    }

    public void Load()
    {
        ItemTypes.CreateType("gun", new GunItem {
            Texture = GunTexture,
            MaxStack=5
        });
    }
}