using Godot;
using System.Collections.Generic;

namespace Recursia;
public partial class DebugDraw : Node
{
    public static DebugDraw? Singleton {get; private set;}

    [Export] public Color Color;

    public bool Draw;
    private ImmediateMesh geometry = new();
    private readonly List<Box> drawing = new ();

    public override void _EnterTree()
    {
        Singleton = this;
    }

    public override void _Process(double delta)
    {
        geometry.ClearSurfaces();
        if (!Draw) return;
        foreach (var b in drawing)
        {
            drawBox(b, geometry);
        }
        if (Player.LocalPlayer?.World != null)
        {
            foreach (var p in Player.LocalPlayer.World.Entities.GetPhysicsObjectsInRange(Player.LocalPlayer.Position, 1000))
            {
                drawBox(p.GetBox(), geometry);
            }
        }

        drawing.Clear();
    }

    public void DrawBox(Box box)
    {
        drawing.Add(box);
    }

    private void drawBox(Box box, ImmediateMesh onto)
    {
        onto.SurfaceBegin(Mesh.PrimitiveType.Lines);
        geometry.SurfaceSetColor(Color);
        //face
        onto.SurfaceAddVertex(box.Corner);
        onto.SurfaceAddVertex(box.Corner+new Vector3(box.Size.X,0,0));

        onto.SurfaceAddVertex(box.Corner+new Vector3(box.Size.X,0,0));
        onto.SurfaceAddVertex(box.Corner+new Vector3(box.Size.X,box.Size.Y,0));

        onto.SurfaceAddVertex(box.Corner+new Vector3(box.Size.X,box.Size.Y,0));
        onto.SurfaceAddVertex(box.Corner+new Vector3(0,box.Size.Y,0));

        onto.SurfaceAddVertex(box.Corner+new Vector3(0,box.Size.Y,0));
        onto.SurfaceAddVertex(box.Corner);
        //face
        onto.SurfaceAddVertex(box.Corner);
        onto.SurfaceAddVertex(box.Corner+new Vector3(0,0,box.Size.Z));

        onto.SurfaceAddVertex(box.Corner+new Vector3(0,0,box.Size.Z));
        onto.SurfaceAddVertex(box.Corner+new Vector3(0,box.Size.Y,box.Size.Z));

        onto.SurfaceAddVertex(box.Corner+new Vector3(0,box.Size.Y,box.Size.Z));
        onto.SurfaceAddVertex(box.Corner+new Vector3(0,box.Size.Y,0));

        onto.SurfaceAddVertex(box.Corner+new Vector3(0,box.Size.Y,0));
        onto.SurfaceAddVertex(box.Corner);
        //face
        onto.SurfaceAddVertex(box.Corner);
        onto.SurfaceAddVertex(box.Corner+new Vector3(box.Size.X,0,0));

        onto.SurfaceAddVertex(box.Corner+new Vector3(box.Size.X,0,0));
        onto.SurfaceAddVertex(box.Corner+new Vector3(box.Size.X,0,box.Size.Z));

        onto.SurfaceAddVertex(box.Corner+new Vector3(box.Size.X,0,box.Size.Z));
        onto.SurfaceAddVertex(box.Corner+new Vector3(0,0,box.Size.Z));

        onto.SurfaceAddVertex(box.Corner+new Vector3(0,0,box.Size.Z));
        onto.SurfaceAddVertex(box.Corner);

        //face
        onto.SurfaceAddVertex(box.Corner + box.Size);
        onto.SurfaceAddVertex(box.Corner+new Vector3(0,box.Size.Y,box.Size.Z));

        onto.SurfaceAddVertex(box.Corner+new Vector3(0,box.Size.Y,box.Size.Z));
        onto.SurfaceAddVertex(box.Corner+new Vector3(0,0,box.Size.Z));

        onto.SurfaceAddVertex(box.Corner+new Vector3(0,0,box.Size.Z));
        onto.SurfaceAddVertex(box.Corner+new Vector3(box.Size.X,0,box.Size.Z));

        onto.SurfaceAddVertex(box.Corner+new Vector3(box.Size.X,0,box.Size.Z));
        onto.SurfaceAddVertex(box.Corner + box.Size);
        //face
        onto.SurfaceAddVertex(box.Corner + box.Size);
        onto.SurfaceAddVertex(box.Corner+new Vector3(box.Size.X,box.Size.Y,0));

        onto.SurfaceAddVertex(box.Corner+new Vector3(box.Size.X,box.Size.Y,0));
        onto.SurfaceAddVertex(box.Corner+new Vector3(box.Size.X,0,0));

        onto.SurfaceAddVertex(box.Corner+new Vector3(box.Size.X,0,0));
        onto.SurfaceAddVertex(box.Corner+new Vector3(box.Size.X,0,box.Size.Z));

        onto.SurfaceAddVertex(box.Corner+new Vector3(box.Size.X,0,box.Size.Z));
        onto.SurfaceAddVertex(box.Corner + box.Size);
        //face
        onto.SurfaceAddVertex(box.Corner + box.Size);
        onto.SurfaceAddVertex(box.Corner+new Vector3(0,box.Size.Y,box.Size.Z));

        onto.SurfaceAddVertex(box.Corner+new Vector3(0,box.Size.Y,box.Size.Z));
        onto.SurfaceAddVertex(box.Corner+new Vector3(0,box.Size.Y,0));

        onto.SurfaceAddVertex(box.Corner+new Vector3(0,box.Size.Y,0));
        onto.SurfaceAddVertex(box.Corner+new Vector3(box.Size.X,box.Size.Y,0));

        onto.SurfaceAddVertex(box.Corner+new Vector3(box.Size.X,box.Size.Y,0));
        onto.SurfaceAddVertex(box.Corner + box.Size);

        onto.SurfaceEnd();
    }
}