using Godot;
using System;

namespace Recursia;
public partial class BossUI : Control
{
    public static BossUI Singleton {get; private set;}

    [Export] public NodePath HealthBarPath;
    [Export] public NodePath NameLabelPath;

    private HealthBar healthBar;
    private Label nameLabel;

    public override void _EnterTree()
    {
        Singleton = this;
    }
    public override void _Ready()
    {
        healthBar = GetNode<HealthBar>(HealthBarPath);
        nameLabel = GetNode<Label>(NameLabelPath);
    }
    public override void _Process(double delta)
    {
        if (healthBar.Tracking != null && !IsInstanceValid(healthBar.Tracking)) Untrack();
    }

    public void Track(Combatant c, string displayName)
    {
        healthBar.Tracking = c;
        nameLabel.Text = displayName;
        Visible = true;
    }

    public void Untrack()
    {
        Visible = false;
        healthBar.Tracking = null;
    }
}
