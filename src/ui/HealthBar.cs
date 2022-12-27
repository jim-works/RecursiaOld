using Godot;
using System;

public class HealthBar : Node
{
    [Export]
    public Combatant Tracking;

    private ColorRect healthbar;
    private ColorRect background;

    public override void _Ready()
    {
        base._Ready();
        healthbar = GetNode<ColorRect>("Health");
        background = GetNode<ColorRect>("Background");
    }

    public override void _Process(float delta)
    {
        if (Tracking == null) {
            GD.PrintErr("HealthBar tracking null object!");
            return;
        }
        float proportion = Tracking.GetHealth()/Tracking.GetMaxHealth();
        healthbar.MarginRight = proportion*background.MarginRight;
    }
}
