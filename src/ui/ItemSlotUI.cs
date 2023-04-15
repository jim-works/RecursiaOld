using Godot;

namespace Recursia;
public partial class ItemSlotUI : Control
{
    private TextureRect itemTex = null!;
    private Label countLabel = null!;
    //argument is button index
    public event System.Action<MouseButton>? OnClick;

    public override void _Ready()
    {
        itemTex = GetNode<TextureRect>("ItemTex");
        countLabel = GetNode<Label>("CountLabel");
        base._Ready();
    }
    public void DisplayItem(ItemStack stack)
    {
        itemTex.Texture = stack.Item.Texture2D;
        #pragma warning disable CA1305
        countLabel.Text = stack.IsEmpty ? "" : stack.Size.ToString();
        #pragma warning restore CA1305
    }

    public void OnGUIInput(InputEvent e)
    {
        if (e is InputEventMouseButton click && click.Pressed) {
            OnClick?.Invoke(click.ButtonIndex);
        }
    }
}