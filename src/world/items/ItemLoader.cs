using Godot;

//TODO: refactor to data loader
public class ItemLoader : Node
{
    [Export]
    public Texture GunTexture;

    public override void _EnterTree()
    {
        BlockLoader.Load();
        Load();
        base._EnterTree();
    }

    public void Load()
    {
        ItemTypes.CreateType("gun", new GunItem {
            Texture = GunTexture,
            MaxStack=5,
            Name="gun"
        });

        RecipeLoader.Load();
    }
}