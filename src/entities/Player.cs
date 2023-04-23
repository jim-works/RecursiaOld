using System.IO;
using Godot;

namespace Recursia;
public partial class Player : Combatant
{
    public static Player? LocalPlayer {get; private set;}
    public static event System.Action<Player>? OnLocalPlayerAssigned;
    [Export] public float Reach = 100;
    [Export] public float MoveSpeed = 10;
    [Export] public float JumpHeight = 10;
    [Export] public Vector3 CameraOffset = new(0, 0.7f, 0);
    [Export] public int InitialInventorySize = 10;
    [Export] public int JumpCount = 1;

    public Inventory MouseInventory = new(1);

    private int SelectedSlot;
    private int jumpsLeft;

    public override void _Ready()
    {
        //temporary, so idc about nullable
        if (Inventory == null)
        {
            Inventory = new Inventory(InitialInventorySize);
            ItemTypes.TryGet("gun", out Item? gun);
            ItemTypes.TryGet("marp_rod", out Item? marp_rod);
            ItemTypes.TryGet("explosive_bullet", out Item? explosive_bullet);
            ItemTypes.TryGet("cursed_idol", out Item? cursed_idol);
            Inventory.CopyItem(new ItemStack { Item = gun!, Size = 1 });
            Inventory.CopyItem(new ItemStack { Item = marp_rod!, Size = 100 });
            Inventory.CopyItem(new ItemStack { Item = cursed_idol!, Size = 3 });
            Inventory.CopyItem(new ItemStack { Item = explosive_bullet!, Size = 100 });
            if (ItemTypes.TryGetBlockItem("loot", out BlockFactoryItem? lootBlockItem))
            {
                lootBlockItem.InitPlaced = (Block block) =>
                {
                    if (block is LootBlock b)
                    {
                        b.Drops = new ItemStack[] { new ItemStack { Item = marp_rod!, Size = 1 } };
                    }
                    else
                    {
                        GD.PushError("Initplaced passed a block which is not a loot block!");
                    }
                };
                Inventory.CopyItem(new ItemStack { Item = lootBlockItem, Size = 1 });
            }
        }
        World?.Loader.AddChunkLoader(this);
        LocalPlayer = this;
        OnLocalPlayerAssigned?.Invoke(this);
        jumpsLeft = JumpCount;
        base._Ready();
    }

    public override void _ExitTree()
    {
        LocalPlayer = null;
        World?.Loader.RemoveChunkLoader(this);
        base._ExitTree();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed)
        {
            if (key.Keycode == Key.Key1)
            {
                SelectedSlot = 0;
                GD.Print("Selected slot 0!");
            }
            else if (key.Keycode == Key.Key2)
            {
                SelectedSlot = 1;
                GD.Print("Selected slot 1!");
            }
            else if (key.Keycode == Key.P)
            {
                if (BlockTypes.TryGet("loot", out Block? b))
                {
                    //temp idc
                    ItemTypes.TryGet("marp_rod", out Item? marp_rod);
                    (b as LootBlock)!.Drops = new ItemStack[] {new ItemStack{Item=marp_rod!,Size=1}};
                    World!.Chunks.SetBlock((BlockCoord)GlobalPosition, b);
                }
            } else if (key.Keycode == Key.T) {
                Collides = !Collides;
            } else if (key.Keycode == Key.Y) {
                TakeDamage(new Damage{Amount=1,Team=null});
            }
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
        BlockcastHit? hit = World?.Blockcast(GlobalPosition + CameraOffset, dir * Reach);
        if (hit != null)
        {
            ItemStack drop = World!.BreakBlock(hit.BlockPos); //if hit isn't null, world isn't null
            if (Inventory == null)
            {
                GD.PushError($"Player {Name} has null inventory!");
                return;
            }
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
        BlockcastHit? hit = World?.Blockcast(GlobalPosition + CameraOffset, dir * Reach);
        if (hit?.Block?.Usable == true)
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
    public override void Serialize(BinaryWriter bw)
    {
        base.Serialize(bw);
        MouseInventory.Serialize(bw);
        bw.Write(jumpsLeft);
    }
    public override void Deserialize(BinaryReader br)
    {
        base.Deserialize(br);
        MouseInventory.Deserialize(br);
        jumpsLeft = br.ReadInt32();
    }
}