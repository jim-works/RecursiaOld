using Godot;

public class MarpRodItem : Item
{
    public PackedScene MarpScene;

    public override void OnUse(Combatant user, Vector3 position, Vector3 dir, ref ItemStack source)
    {
        Marp marp = MarpScene.Instance<Marp>();
        marp.Position = position;
        marp.Team = user.Team;
        World.Singleton.AddChild(marp);

        source.Size--;

        base.OnUse(user, position, dir, ref source);
    }
}