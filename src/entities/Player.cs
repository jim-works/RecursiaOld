using Godot;

public class Player : Combatant
{
    [Export]
    public float Reach = 100;
    [Export]
    public float MoveSpeed = 10;
    [Export]
    public float JumpHeight = 10;
    [Export]
    public Vector3 CameraOffset = new Vector3(0,0.7f,0);
    [Export]
    public int InitialInventorySize = 10;
    
    public Inventory MouseInventory = new Inventory(1);

    private int SelectedSlot = 0;

    public override void _EnterTree()
    {
        Inventory = new Inventory(InitialInventorySize);
        World.Singleton.ChunkLoaders.Add(this);
        World.Singleton.Players.Add(this);
        base._EnterTree();
    }

    public override void _ExitTree()
    {
        World.Singleton.Players.Remove(this);
        World.Singleton.ChunkLoaders.Remove(this);
        base._ExitTree();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed)
        if ((KeyList)key.Scancode == KeyList.Key1)
        {
            SelectedSlot = 0;
            GD.Print("Selected slot 0!");
        }
        else if ((KeyList)key.Scancode == KeyList.Key2)
        {
            SelectedSlot = 1;
            GD.Print("Selected slot 1!");
        }
        else if ((KeyList)key.Scancode == KeyList.P)
        {
            ItemStack stack = new ItemStack {Item=ItemTypes.GetBlockItem("dirt"),Size=1};
            Inventory.AddItem(ref stack);
        }
        base._Input(@event);
    }

    public override void _Process(float delta)
    {
        move(delta);

        base._Process(delta);
    }

    //called by rotating camera
    public void Punch(Vector3 dir)
    {
        BlockcastHit hit = World.Singleton.Blockcast(Position+CameraOffset, dir*Reach);
        if (hit != null) {
            ItemStack drop = World.Singleton.BreakBlock(hit.BlockPos);
            Inventory.AddItem(ref drop);
        }
    }

    //called by rotating camera
    public void Use(Vector3 dir)
    {
        Inventory.GetItem(SelectedSlot).Item?.OnUse(this, Position+CameraOffset, dir, ref Inventory.Items[SelectedSlot]);
        Inventory.TriggerUpdate();
    }

    private void move(float delta)
    {
        float x = Input.GetActionStrength("move_right")-Input.GetActionStrength("move_left");
        float z = Input.GetActionStrength("move_backward")-Input.GetActionStrength("move_forward");
        Vector3 movement =  LocalDirectionToWorld(new Vector3(MoveSpeed*x,0,MoveSpeed*z));
        Velocity.x = movement.x;
        Velocity.z = movement.z;
        if (Input.IsActionJustPressed("jump"))
        {
            Velocity.y = JumpHeight;
        }
    }
}