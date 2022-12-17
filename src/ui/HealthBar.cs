using Godot;
using System;

public class HealthBar : Node
{
    [Export]
    public Combatant Tracking;
    [Export]
    public float Stiffness = 1;

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
        float proportion = Tracking.Health/Tracking.MaxHealth;

        healthbar.MarginRight = Mathf.Lerp(healthbar.MarginRight, proportion*background.MarginRight, Stiffness*delta);
    }
}
