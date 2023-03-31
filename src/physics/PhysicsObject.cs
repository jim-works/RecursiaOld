using Godot;
using System.IO;
using System.Runtime.CompilerServices;

public partial class PhysicsObject : Node3D, ISerializable
{
    private const int COLLISION_INTERVAL = 5; //physics updates per collision check
    [Export]
    public Vector3 Size;
    [Export] public Vector3 ColliderOffset;
    [Export]
    //reduces collision planes' sizes by this amount in each direction to avoid getting stuck on things.
    //For example, it would stop an x-axis collision to be detected while walking on flat ground.
    public float Epsilon = 0.1f;
    [Export]
    public float Mass = 1f;
    public Vector3 Velocity;
    [Export]
    public Vector3 Gravity = new Vector3(0,-20,0);
    [Export]
    public float AirResistance = 0.1f;
    [Export]
    public float MaxSpeed = 100f;
    [Export] public bool PhysicsActive = true;
    [Export] public Vector3 InitialPosition;

    public event System.Action<PhysicsObject, ChunkCoord> OnCrossChunkBoundary;
    public event System.Action<PhysicsObject> OnExitTree;

    public ChunkCoord OldCoord;
    public bool Registered = false;

    public bool Collides = true;

    protected Vector3 currentForce; //zeroed each physics update
    protected int collisionDirections = 0; //updated each physics update, bitmask of Directions of current collision with world

    private int _updatesSinceCollision = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Box GetBox() => Box.FromCenter(GlobalPosition+LocalDirectionToWorld(ColliderOffset), Size);

    public override void _EnterTree()
    {
        GlobalPosition = InitialPosition;
        OldCoord = (ChunkCoord)GlobalPosition;
        if (!Registered) World.Singleton.RegisterObject(this); //used for objects created in editor (ex player, patrick quack's limbs)
        
        base._EnterTree();
    }

    public override void _PhysicsProcess(double dt)
    {
        if (OldCoord != (ChunkCoord)GlobalPosition)
        {
            OnCrossChunkBoundary?.Invoke(this, OldCoord);
            OldCoord = (ChunkCoord)GlobalPosition;
        }
        if (!PhysicsActive) return;
        AddConstantForce(-AirResistance*Velocity);
        Velocity += currentForce*(float)dt/Mass;
        if (Velocity.LengthSquared() > MaxSpeed*MaxSpeed) Velocity=Velocity.Normalized()*MaxSpeed;
        //GD.Print(Velocity);
        currentForce = Gravity*Mass;
        if (Collides) doCollision(World.Singleton, (float)dt);

        GlobalPosition += Velocity*(float)dt;
    }

    public override void _ExitTree()
    {
        OnExitTree?.Invoke(this);
        base._ExitTree();
    }

    //adds a force for the next physics update, does not persist across updates
    //world space
    public void AddConstantForce(Vector3 f)
    {
        currentForce += f;
    }
    //adds an impulse. instant velocity change that accounts for mass.
    public void AddImpulse(Vector3 f)
    {
        Velocity += f/Mass;
    }

    //rotates v from local to world
    public Vector3 LocalDirectionToWorld(Vector3 v)
    {
        return GlobalTransform.Basis * v;
    }
    protected void doFriction(float coeff)
    {
        Velocity -= Velocity*coeff;
    }
    protected virtual void doCollision(World world, float dt)
    {
        _updatesSinceCollision++;
        if (_updatesSinceCollision > COLLISION_INTERVAL) _updatesSinceCollision = 0;
        Vector3 oldV = Velocity;
        int oldMask = collisionDirections;
        collisionDirections = 0;
        Vector3 frameVelocity = Velocity * dt; //this is the amount the object will move this update
        Vector3 postPosition = GlobalPosition;
        //we're going to check collision by iterating through each plane orthogonal to the Velocity the the three axes directions.
        //we only have to check each plane of blocks that the frame Velocity vector hits.

        //y axis
        if (frameVelocity.Y < 0)
        {
            //moving down
            for (int y = 0; y > Mathf.FloorToInt(frameVelocity.Y); y--)
            {
                if (Plane.CollidesWithWorldY(GlobalPosition + new Vector3(-Size.X/2,-Size.Y/2+y, -Size.Z/2), Size.X, Size.Z, world))
                {
                    postPosition.Y = Mathf.CeilToInt(GlobalPosition.Y-Size.Y/2+y)+Size.Y/2;
                    Velocity.Y = 0;
                    collisionDirections |= (1<<(int)Direction.NegY);
                    break;
                }
            }
        }
        else
        {
            //moving up
            for (int y = 0; y < Mathf.CeilToInt(frameVelocity.Y); y++)
            {
                if (Plane.CollidesWithWorldY(GlobalPosition + new Vector3(-Size.X/2,Size.Y/2+y, -Size.Z/2), Size.X, Size.Z, world))
                {
                    postPosition.Y = Mathf.FloorToInt(GlobalPosition.Y+Size.Y/2+y)-Size.Y/2;
                    Velocity.Y = 0;
                    collisionDirections |= (1<<(int)Direction.PosY);
                    break;
                }
            }
        }

        //x axis
        if (frameVelocity.X < 0)
        {
            //moving left
            for (int x = 0; x > Mathf.FloorToInt(frameVelocity.X); x--)
            {
                if (Plane.CollidesWithWorldX(GlobalPosition + new Vector3(-Size.X/2+x,-Size.Y/2+Epsilon, -Size.Z/2+Epsilon), Size.Y-2*Epsilon, Size.Z-2*Epsilon, world))
                {
                    //postPosition.X = Mathf.CeilToInt(GlobalPosition.X-Size.X/2+x)+Size.X/2;
                    Velocity.X = 0;
                    collisionDirections |= (1<<(int)Direction.NegX);
                    break;
                }
            }
        }
        else
        {
            //moving right
            for (int x = 0; x < Mathf.CeilToInt(frameVelocity.X); x++)
            {
                if (Plane.CollidesWithWorldX(GlobalPosition + new Vector3(Size.X/2+x,-Size.Y/2+Epsilon, -Size.Z/2+Epsilon), Size.Y-2*Epsilon, Size.Z-2*Epsilon, world))
                {
                    postPosition.X = Mathf.FloorToInt(GlobalPosition.X+Size.X/2+x)-Size.X/2;
                    Velocity.X = 0;
                    collisionDirections |= (1<<(int)Direction.PosX);
                    break;
                }
            }
        }
        //z azis
        if (frameVelocity.Z < 0)
        {
            //moving forward
            for (int z = 0; z > Mathf.FloorToInt(frameVelocity.Z); z--)
            {
                if (Plane.CollidesWithWorldZ(GlobalPosition + new Vector3(-Size.Z/2+Epsilon,-Size.Y/2+Epsilon, -Size.Z/2+z), Size.X-2*Epsilon,Size.Y-2*Epsilon, world))
                {
                    //postPosition.Z = Mathf.CeilToInt(GlobalPosition.Z-Size.Z/2+z)+Size.Z/2;
                    Velocity.Z = 0;
                    collisionDirections |= (1<<(int)Direction.NegZ);
                    break;
                }
            }
        }
        else
        {
            //moving backward
            for (int z = 0; z < Mathf.CeilToInt(frameVelocity.Z); z++)
            {
                if (Plane.CollidesWithWorldZ(GlobalPosition + new Vector3(-Size.X/2+Epsilon,-Size.Y/2+Epsilon, Size.Z/2+z), Size.X-2*Epsilon, Size.Y-2*Epsilon, world))
                {
                    postPosition.Z = Mathf.FloorToInt(GlobalPosition.Z+Size.Z/2+z)-Size.Z/2;
                    Velocity.Z = 0;
                    collisionDirections |= (1<<(int)Direction.PosZ);
                    break;
                }
            }
        }

        GlobalPosition = postPosition;
    }

    public virtual void Serialize(BinaryWriter writer)
    {
        GlobalPosition.Serialize(writer);
        Velocity.Serialize(writer);
        
    }
}