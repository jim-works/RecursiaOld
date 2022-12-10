using Godot;
using System.Runtime.CompilerServices;

public class PhysicsObject : Spatial
{
    public Vector3 Size;
    public Vector3 Velocity;
    public Vector3 Gravity = new Vector3(0,-10,0);
    public bool PhysicsActive = true;

    protected Vector3 currentForce; //zeroed each physics update
    protected int collisionDirections = 0; //updated each physics update, bitmask of Directions of current collision with world

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Box GetBox() => Box.FromCenter(GlobalTransform.origin, Size);

    public override void _PhysicsProcess(float delta)
    {
        if (!PhysicsActive) return;
        Velocity += currentForce*delta;
        //GD.Print(Velocity);
        currentForce = Gravity;
        handleCollision(World.Singleton); //between velocity update and position update to guarantee we are never inside a wall
        Translation += Velocity*delta;
    }

    //adds a force for the next physics update, does not persist across updates
    public void AddForce(Vector3 f)
    {
        currentForce += f;
    }

    //rotates v from local to world
    //TODO ACTUALLY MAKE THIS WORK ROFLCOPTER
    public Vector3 LocalDirectionToWorld(Vector3 v)
    {
        return GlobalTransform.basis.z*v.z;
    }

    //doesn't acccount for velocity
    private int handleCollision(World world)
    {
        collisionDirections = 0;
        //check each axis independently
        if (checkNegXCollision(world, out int nx)) {
            collisionDirections |= 1 << (int)Direction.NegX;
            Velocity.x = Mathf.Max(0,Velocity.x);
            Translation = new Vector3(nx, Translation.y, Translation.z);
        }
        if (checkPosXCollision(world, out int px)) {
            collisionDirections |= 1 << (int)Direction.PosX;
            Velocity.x = Mathf.Min(0,Velocity.x);
            Translation = new Vector3(px, Translation.y, Translation.z);
        }
        if (checkNegYCollision(world, out int ny)) {
            collisionDirections |= 1 << (int)Direction.NegY;
            Velocity.y = Mathf.Max(0,Velocity.y);
            Translation = new Vector3(Translation.x, ny, Translation.z);
        }
        if (checkPosYCollision(world, out int py)) {
            collisionDirections |= 1 << (int)Direction.PosY;
            Velocity.y = Mathf.Min(0,Velocity.y);
            Translation = new Vector3(Translation.x, py, Translation.z);
        }
        if (checkNegZCollision(world, out int nz)) {
            collisionDirections |= 1 << (int)Direction.NegZ;
            Velocity.z = Mathf.Max(0,Velocity.z);
            Translation = new Vector3(Translation.x, Translation.y, nz);
        }
        if (checkPosZCollision(world, out int pz)) {
            collisionDirections |= 1 << (int)Direction.PosZ;
            Velocity.z = Mathf.Min(0,Velocity.z);
            Translation = new Vector3(Translation.x, Translation.y, pz);
        }

        return collisionDirections;
    }

    private bool checkNegXCollision(World world, out int coord)
    {
        coord = Mathf.CeilToInt(GetBox().Corner.x);
        return Plane.CollidesWithWorldX(GetBox().Corner, Size.y, Size.z, world);
    }
    private bool checkPosXCollision(World world, out int coord)
    {
        coord = Mathf.FloorToInt((GetBox().Corner.x+Size.x));
        return Plane.CollidesWithWorldX(GetBox().Corner+new Vector3(Size.x,0,0), Size.y, Size.z, world);
    }
    private bool checkNegYCollision(World world, out int coord)
    {
        coord = Mathf.CeilToInt(GetBox().Center().y);
        return Plane.CollidesWithWorldY(GetBox().Corner, Size.y, Size.z, world);
    }
    private bool checkPosYCollision(World world, out int coord)
    {
        coord = Mathf.FloorToInt(GetBox().Corner.y+Size.y);
        return Plane.CollidesWithWorldY(GetBox().Corner+new Vector3(0,Size.y,0), Size.y, Size.z, world);
    }
    private bool checkNegZCollision(World world, out int coord)
    {
        coord = Mathf.CeilToInt(GetBox().Corner.z);
        return Plane.CollidesWithWorldZ(GetBox().Corner, Size.y, Size.z, world);
    }
    private bool checkPosZCollision(World world, out int coord)
    {
        coord = Mathf.FloorToInt(GetBox().Corner.z+Size.z);
        return Plane.CollidesWithWorldZ(GetBox().Corner+new Vector3(0,0,Size.z), Size.y, Size.z, world);
    }
}