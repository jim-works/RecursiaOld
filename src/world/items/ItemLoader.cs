using Godot;

//TODO: refactor to data loader
public partial class ItemLoader : Node
{
    [Export] public Texture2D GunTexture;
    [Export] public Texture2D ShotgunTexture;
    [Export] public Texture2D ExplosiveBulletTexture;
    [Export] public Texture2D TrackingBulletTexture;
    [Export] public Texture2D MarpRodTexture;
    [Export] public Texture2D CursedIdolTexture;

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
            Texture2D = GunTexture,
            MaxStack=1,
            DisplayName="gun",
            Damage=2,
            Cooldown=0.25f,
            UseSound = GunSound,
        });
        ItemTypes.CreateType("shotgun", new ShotgunItem {
            Texture2D = ShotgunTexture,
            MaxStack=1,
            DisplayName="shotgun",
            Damage=0,
            Cooldown=1,
            UseSound = ShotgunSound,
        });
        ItemTypes.CreateType("explosive_bullet", new BulletItem {
            Texture2D=ExplosiveBulletTexture,
            DisplayName="explosive bullet",
            Damage=5,
            ProjectileScene = GD.Load<PackedScene>("res://objects/ExplosiveBullet.tscn")
        });
        ItemTypes.CreateType("tracking_bullet", new BulletItem {
            Texture2D=TrackingBulletTexture,
            DisplayName="tracking bullet",
            Damage=1,
            ProjectileScene = GD.Load<PackedScene>("res://objects/TrackingBullet.tscn")
        });
        ItemTypes.CreateType("marp_rod", new SummoningItem {
            Texture2D=MarpRodTexture,
            DisplayName="marp rod",
            ToSummon = GD.Load<PackedScene>("res://objects/enemies/Marp.tscn"),
            SetToUserTeam = true,
            Distance = 5,
            UseSound = MarpRodSound,
        });
        ItemTypes.CreateType("cursed_idol", new SummoningItem {
            Texture2D=CursedIdolTexture,
            DisplayName="cursed idol",
            ToSummon = GD.Load<PackedScene>("res://objects/enemies/PatrickQuack.tscn"),
            SetToUserTeam = false,
            Distance = 75,
            UseSound = CursedIdolSound,
        });

        RecipeLoader.Load();
    }
}