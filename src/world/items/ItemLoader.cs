using Godot;

//TODO: refactor to data loader
public class ItemLoader : Node
{
    [Export] public Texture GunTexture;
    [Export] public Texture ShotgunTexture;
    [Export] public Texture ExplosiveBulletTexture;
    [Export] public Texture TrackingBulletTexture;

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
            MaxStack=1,
            Name="gun",
            Damage=0,
        });
        ItemTypes.CreateType("shotgun", new ShotgunItem {
            Texture = ShotgunTexture,
            MaxStack=1,
            Name="shotgun",
            Damage=0,
        });
        ItemTypes.CreateType("explosive_bullet", new BulletItem {
            Texture=ExplosiveBulletTexture,
            Name="explosive bullet",
            Damage=5,
            ProjectileScene = GD.Load<PackedScene>("res://objects/ExplosiveBullet.tscn")
        });
        ItemTypes.CreateType("tracking_bullet", new BulletItem {
            Texture=TrackingBulletTexture,
            Name="tracking bullet",
            Damage=1,
            ProjectileScene = GD.Load<PackedScene>("res://objects/TrackingBullet.tscn")
        });


        RecipeLoader.Load();
    }
}