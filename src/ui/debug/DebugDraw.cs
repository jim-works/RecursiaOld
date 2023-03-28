using Godot;
using System.Collections.Generic;

public partial class DebugDraw : Node
{
    public static DebugDraw Singleton;

    [Export] public Color Color;

    public bool Draw = false;
    private ImmediateMesh geometry;
    private List<Box> drawing = new List<Box>();

    public override void _EnterTree()
    {
        Singleton = this;
    }

    public override void _Ready()
    {
        geometry = new ImmediateMesh();
    }

    public override void _Process(double dt)
    {
        geometry.ClearSurfaces();
        if (!Draw) return;
        foreach (var b in drawing)
        {
            drawBox(b, geometry);
        }
        foreach (var l in World.Singleton.PhysicsObjects.Values)
        foreach (var c in l)
        {
            drawBox(c.GetBox(), geometry);
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