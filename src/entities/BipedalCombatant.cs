using Godot;

public class BipedalCombatant : Combatant
{
    [Export] public NodePath AnimationPlayerPath;
    [Export] public string WalkAnimation = "walk-loop";
    [Export] public float StrideLength = 40;

    private AnimationPlayer animationPlayer;

    public override void _Ready()
    {
        animationPlayer = GetNode<AnimationPlayer>(AnimationPlayerPath);
        animationPlayer.Play(WalkAnimation);
        base._Ready();
    }

    public override void _PhysicsProcess(float dt)
    {
        if (animationPlayer.CurrentAnimation == WalkAnimation) animationPlayer.PlaybackSpeed = new Vector3(Velocity.x,0,Velocity.z).Length()/StrideLength;
        Velocity = new Vector3(0,0,10);
        base._PhysicsProcess(dt);
    }
}