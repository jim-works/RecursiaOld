using Godot;

//TODO: refactor to data loader
public class ItemLoader : Node
{
    [Export] public Texture GunTexture;
    [Export] public Texture ShotgunTexture;
    [Export] public Texture ExplosiveBulletTexture;
    [Export] public Texture TrackingBulletTexture;
    [Export] public Texture MarpRodTexture;
    [Export] public Texture CursedIdolTexture;

    [Export] public AudioStream GunSound;
    [Export] public AudioStream ShotgunSound;
    [Export] public AudioStream MarpRodSound;
    [Export] public AudioStream CursedIdolSound;

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
            Damage=2,
            Cooldown=0.25f,
            UseSound = GunSound,
        });
        ItemTypes.CreateType("shotgun", new ShotgunItem {
            Texture = ShotgunTexture,
            MaxStack=1,
            Name="shotgun",
            Damage=0,
            Cooldown=1,
            UseSound = ShotgunSound,
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
        ItemTypes.CreateType("marp_rod", new SummoningItem {
            Texture=MarpRodTexture,
            Name="marp rod",
            ToSummon = GD.Load<PackedScene>("res://objects/enemies/Marp.tscn"),
            SetToUserTeam = true,
            Distance = 5,
            UseSound = MarpRodSound,
        });
        ItemTypes.CreateType("cursed_idol", new SummoningItem {
            Texture=CursedIdolTexture,
            Name="cursed idol",
            ToSummon = GD.Load<PackedScene>("res://objects/enemies/PatrickQuack.tscn"),
            SetToUserTeam = false,
            Distance = 75,
            UseSound = CursedIdolSound,
        });

        RecipeLoader.Load();
    }
}