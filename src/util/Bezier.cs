using Godot;

//quadratic bezier curve
//https://en.wikipedia.org/wiki/B%C3%A9zier_curve#Quadratic_B%C3%A9zier_curves
namespace Recursia;
public struct Bezier
{
    public Vector3 start;
    public Vector3 control;
    public Vector3 end;

    public Bezier(Vector3 start, Vector3 control, Vector3 end)
    {
        this.start = start;
        this.control = control;
        this.end = end;
    }

    //evaluate between 0 and 1, but doesn't check bounds
    public Vector3 Sample(float t)
    {
        return (1-t)*(1-t)*start+2*(1-t)*control+t*t*end;
    }

    //derivative wrt t
    public Vector3 Dt(float t)
    {
        return 2*(1-t)*(control-start)+2*t*(end-control);
    }

    //second derivative wrt t
    public Vector3 Dt2()
    {
        return 2*(end-2*control+start);
    }
}