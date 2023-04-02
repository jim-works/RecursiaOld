using Godot;

public partial class Player : Combatant
{
    [Export] public float Reach = 100;
    [Export] public float MoveSpeed = 10;
    [Export] public float JumpHeight = 10;
    [Export] public Vector3 CameraOffset = new Vector3(0, 0.7f, 0);
    [Export] public int InitialInventorySize = 10;
    [Export] public int JumpCount = 1;

    public Inventory MouseInventory = new Inventory(1);

    private int SelectedSlot = 0;
    private int jumpsLeft = 0;

    public override void _Ready()
    {
        World.Singleton.RegisterObject(this); //necessary for now since player is added in editor instead of created using World.SpawnObject
        Inventory = new Inventory(InitialInventorySize);
        Inventory.CopyItem(new ItemStack { Item = ItemTypes.Get("gun"), Size = 1 });
        Inventory.CopyItem(new ItemStack { Item = ItemTypes.Get("explosive_bullet"), Size = 100 });
        Inventory.CopyItem(new ItemStack { Item = ItemTypes.Get("cursed_idol"), Size = 3 });
        Inventory.CopyItem(new ItemStack { Item = ItemTypes.Get("marp_rod"), Size = 1 });
        Inventory.CopyItem(new ItemStack { Item = ItemTypes.GetBlockItem("lava"), Size = 1 });
        BlockFactoryItem lootBlockItem = ItemTypes.GetBlockItem("loot");
        lootBlockItem.InitPlaced = (Block b) => {
            (b as LootBlock).Drops = new ItemStack[] { new ItemStack { Item = ItemTypes.Get("marp_rod"), Size = 1 } };
        };
        Inventory.CopyItem(new ItemStack {Item = lootBlockItem, Size = 1});
        World.Singleton.ChunkLoaders.Add(this);
        World.Singleton.LocalPlayer = this;
        World.Singleton.Players.Add(this);
        jumpsLeft = JumpCount;
        base._Ready();
    }

    public override void _ExitTree()
    {
        World.Singleton.LocalPlayer = null;
        World.Singleton.Players.Remove(this);
        World.Singleton.ChunkLoaders.Remove(this);
        base._ExitTree();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed)
            if ((Key)key.Keycode == Key.Key1)
            {
                SelectedSlot = 0;
                GD.Print("Selected slot 0!");
            }
            else if ((Key)key.Keycode == Key.Key2)
            {
                SelectedSlot = 1;
                GD.Print("Selected slot 1!");
            }
            else if ((Key)key.Keycode == Key.P)
            {
                LootBlock b = (LootBlock)BlockTypes.Get("loot");
                b.Drops = new ItemStack[] {new ItemStack{Item=ItemTypes.Get("marp_rod"),Size=1}};
                World.Singleton.SetBlock((BlockCoord)GlobalPosition, b);
            } else if ((Key)key.Keycode == Key.T) {
                Collides = !Collides;
            }
        base._Input(@event);
    }

    public override void _Process(double delta)
    {
        move();
        base._Process(delta);
    }

    protected override void doCollision(World world, float dt)
    {
        base.doCollision(world, dt);
        if (DirectionUtils.MaskHas(collisionDirections, Direction.NegY))
        {
            jumpsLeft = JumpCount; //refill jumps if on ground
        }
    }

    //called by rotating camera
    public void Punch(Vector3 dir)
    {
        BlockcastHit hit = World.Singleton.Blockcast(GlobalPosition + CameraOffset, dir * Reach);
        if (hit != null)
        {
            ItemStack drop = World.Singleton.BreakBlock(hit.BlockPos);
            Inventory.AddItem(ref drop);
        }
    }

    public void Jump()
    {
        if (jumpsLeft <= 0) return;
        Velocity.Y = JumpHeight;
        jumpsLeft--;
    }

    //called by rotating camera
    public void Use(Vector3 dir)
    {
        BlockcastHit hit = World.Singleton.Blockcast(GlobalPosition + CameraOffset, dir * Reach);
        if (hit != null && hit.Block.Usable)
        {
            hit.Block.OnUse(this, hit.BlockPos);
        }
        else
        {
            UseItem(SelectedSlot, GlobalPosition + CameraOffset, dir);
        }
    }

    private void move()
    {
        float x = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
        float z = Input.GetActionStrength("move_backward") - Input.GetActionStrength("move_forward");
        Vector3 movement = LocalDirectionToWorld(new Vector3(MoveSpeed * x, 0, MoveSpeed * z));
        Velocity.X = movement.X;
        Velocity.Z = movement.Z;
        if (Input.IsActionJustPressed("jump"))
        {
            Jump();
        }
    }
}