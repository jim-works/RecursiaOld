using Godot;

public class ItemSlotUI : Control
{
    private TextureRect itemTex;
    private Label countLabel;
    //argument is button index
    public event System.Action<int> OnClick;

    public override void _Ready()
    {
        itemTex = GetNode<TextureRect>("ItemTex");
        countLabel = GetNode<Label>("CountLabel");
        base._Ready();
    }
    public void DisplayItem(ItemStack stack)
    {
        itemTex.Texture = stack.Item?.Texture;
        countLabel.Text = stack.Item == null ? "" : stack.Size.ToString();
    }

    public void OnGUIInput(InputEvent e)
    {
        if (e is InputEventMouseButton click && click.Pressed) {
            OnClick?.Invoke(click.ButtonIndex);
        }
    }
}