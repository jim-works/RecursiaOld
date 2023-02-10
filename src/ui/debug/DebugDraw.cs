using Godot;
using System.Collections.Generic;

public class DebugDraw : Node
{
    public static DebugDraw Singleton;

    [Export] public Color Color;

    public bool Draw = false;
    private ImmediateGeometry geometry;
    private List<Box> drawing = new List<Box>();

    public override void _EnterTree()
    {
        Singleton = this;
    }

    public override void _Ready()
    {
        geometry = new ImmediateGeometry();
        AddChild(geometry);
    }

    public override void _Process(float dt)
    {
        geometry.Clear();
        if (!Draw) return;
        foreach (var b in drawing)
        {
            drawBox(b, geometry);
        }
        foreach (var c in World.Singleton.PhysicsObjects)
        {
            drawBox(c.GetBox(), geometry);
        }
        drawing.Clear();
    }

    public void DrawBox(Box box)
    {
        drawing.Add(box);
    }

    private void drawBox(Box box, ImmediateGeometry onto)
    {
        onto.Begin(Mesh.PrimitiveType.Lines);
        geometry.SetColor(Color);
        //face
        onto.AddVertex(box.Corner);
        onto.AddVertex(box.Corner+new Vector3(box.Size.x,0,0));
        
        onto.AddVertex(box.Corner+new Vector3(box.Size.x,0,0));
        onto.AddVertex(box.Corner+new Vector3(box.Size.x,box.Size.y,0));

        onto.AddVertex(box.Corner+new Vector3(box.Size.x,box.Size.y,0));
        onto.AddVertex(box.Corner+new Vector3(0,box.Size.y,0));

        onto.AddVertex(box.Corner+new Vector3(0,box.Size.y,0));
        onto.AddVertex(box.Corner);
        //face
        onto.AddVertex(box.Corner);
        onto.AddVertex(box.Corner+new Vector3(0,0,box.Size.z));
        
        onto.AddVertex(box.Corner+new Vector3(0,0,box.Size.z));
        onto.AddVertex(box.Corner+new Vector3(0,box.Size.y,box.Size.z));

        onto.AddVertex(box.Corner+new Vector3(0,box.Size.y,box.Size.z));
        onto.AddVertex(box.Corner+new Vector3(0,box.Size.y,0));

        onto.AddVertex(box.Corner+new Vector3(0,box.Size.y,0));
        onto.AddVertex(box.Corner);
        //face
        onto.AddVertex(box.Corner);
        onto.AddVertex(box.Corner+new Vector3(box.Size.x,0,0));
        
        onto.AddVertex(box.Corner+new Vector3(box.Size.x,0,0));
        onto.AddVertex(box.Corner+new Vector3(box.Size.x,0,box.Size.z));

        onto.AddVertex(box.Corner+new Vector3(box.Size.x,0,box.Size.z));
        onto.AddVertex(box.Corner+new Vector3(0,0,box.Size.z));

        onto.AddVertex(box.Corner+new Vector3(0,0,box.Size.z));
        onto.AddVertex(box.Corner);

        //face
        onto.AddVertex(box.Corner + box.Size);
        onto.AddVertex(box.Corner+new Vector3(0,box.Size.y,box.Size.z));
        
        onto.AddVertex(box.Corner+new Vector3(0,box.Size.y,box.Size.z));
        onto.AddVertex(box.Corner+new Vector3(0,0,box.Size.z));

        onto.AddVertex(box.Corner+new Vector3(0,0,box.Size.z));
        onto.AddVertex(box.Corner+new Vector3(box.Size.x,0,box.Size.z));

        onto.AddVertex(box.Corner+new Vector3(box.Size.x,0,box.Size.z));
        onto.AddVertex(box.Corner + box.Size);
        //face
        onto.AddVertex(box.Corner + box.Size);
        onto.AddVertex(box.Corner+new Vector3(box.Size.x,box.Size.y,0));
        
        onto.AddVertex(box.Corner+new Vector3(box.Size.x,box.Size.y,0));
        onto.AddVertex(box.Corner+new Vector3(box.Size.x,0,0));

        onto.AddVertex(box.Corner+new Vector3(box.Size.x,0,0));
        onto.AddVertex(box.Corner+new Vector3(box.Size.x,0,box.Size.z));

        onto.AddVertex(box.Corner+new Vector3(box.Size.x,0,box.Size.z));
        onto.AddVertex(box.Corner + box.Size);
        //face
        onto.AddVertex(box.Corner + box.Size);
        onto.AddVertex(box.Corner+new Vector3(0,box.Size.y,box.Size.z));
        
        onto.AddVertex(box.Corner+new Vector3(0,box.Size.y,box.Size.z));
        onto.AddVertex(box.Corner+new Vector3(0,box.Size.y,0));

        onto.AddVertex(box.Corner+new Vector3(0,box.Size.y,0));
        onto.AddVertex(box.Corner+new Vector3(box.Size.x,box.Size.y,0));

        onto.AddVertex(box.Corner+new Vector3(box.Size.x,box.Size.y,0));
        onto.AddVertex(box.Corner + box.Size);

        onto.End();
    }
}