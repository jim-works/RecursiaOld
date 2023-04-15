using Godot;
using System;

namespace Recursia;
public partial class HealthBar3D : Node3D
{
    [Export] public HealthBar? SubBar;
    [Export] public SubViewport? subViewport;
    [Export] public Sprite3D? sprite;
    public override void _Ready()
    {
        SubBar!.Tracking = GetParent<Combatant>();
        sprite!.Texture = subViewport!.GetTexture();
        base._Ready();
    }
}
