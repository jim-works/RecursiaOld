using Godot;

//TODO: refactor to data loader
namespace Recursia;
public partial class ItemLoader : Node
{
    [Export] public Texture2D? GunTexture;
    [Export] public Texture2D? ShotgunTexture;
    [Export] public Texture2D? ExplosiveBulletTexture;
    [Export] public Texture2D? TrackingBulletTexture;
    [Export] public Texture2D? MarpRodTexture;
    [Export] public Texture2D? CursedIdolTexture;

    [Export] public AudioStream? GunSound;
    [Export] public AudioStream? ShotgunSound;
    [Export] public AudioStream? MarpRodSound;
    [Export] public AudioStream? CursedIdolSound;

    public override void _EnterTree()
    {
        Load();
        base._EnterTree();
    }

    public void Load()
    {
        ItemTypes.CreateType("gun", new GunItem("gun", "gun") {
            Texture2D = GunTexture,
            MaxStack=1,
            Damage=2,
            Cooldown=0.25f,
            UseSound = GunSound,
        });
        ItemTypes.CreateType("shotgun", new ShotgunItem("shotgun", "shotgun") {
            Texture2D = ShotgunTexture,
            MaxStack=1,
            Damage=0,
            Cooldown=1,
            UseSound = ShotgunSound,
        });
        ItemTypes.CreateType("explosive_bullet", new BulletItem("explosive_bullet", "explosive bullet") {
            Texture2D=ExplosiveBulletTexture,
            Damage=5,
            ProjectileScene = GD.Load<PackedScene>("res://objects/ExplosiveBullet.tscn")
        });
        ItemTypes.CreateType("tracking_bullet", new BulletItem("tracking_bullet", "tracking bullet") {
            Texture2D=TrackingBulletTexture,
            Damage=1,
            ProjectileScene = GD.Load<PackedScene>("res://objects/TrackingBullet.tscn")
        });
        ItemTypes.CreateType("marp_rod", new SummoningItem("marp_rod", "marp rod") {
            Texture2D=MarpRodTexture,
            ToSummon = GD.Load<PackedScene>("res://objects/enemies/Marp.tscn"),
            SetToUserTeam = true,
            Distance = 5,
            UseSound = MarpRodSound,
        });
        ItemTypes.CreateType("cursed_idol", new SummoningItem("cursed_idol", "cursed idol") {
            Texture2D=CursedIdolTexture,
            ToSummon = GD.Load<PackedScene>("res://objects/enemies/PatrickQuack.tscn"),
            SetToUserTeam = false,
            Distance = 75,
            UseSound = CursedIdolSound,
        });

        BlockLoader.LoadAfterItems();
        RecipeLoader.Load();
    }
}