using System.Numerics;

namespace EdgeGlow;

public enum Edge { Top, Bottom, Left, Right }
public record Display(string Id, Rectangle Bounds) { public override string ToString() => $"{Id.Replace(@"\\.\", "")} · {Bounds.Width}×{Bounds.Height} · ({Bounds.X}, {Bounds.Y})"; }
public record Seam(Rectangle Source, Rectangle Target, Edge SourceEdge, int Start, int End)
{
    public bool Horizontal => SourceEdge is Edge.Top or Edge.Bottom;
    public Edge TargetEdge => SourceEdge switch { Edge.Top => Edge.Bottom, Edge.Bottom => Edge.Top, Edge.Left => Edge.Right, _ => Edge.Left };
    public Rectangle Band(float depth)
    {
        int d = Math.Max(2, (int)Math.Round((Horizontal ? Target.Height : Target.Width) * depth));
        return TargetEdge switch {
            Edge.Bottom => new(Target.Left, Target.Bottom-d, Target.Width, d),
            Edge.Top => new(Target.Left, Target.Top, Target.Width, d),
            Edge.Right => new(Target.Right-d, Target.Top, d, Target.Height),
            _ => new(Target.Left, Target.Top, d, Target.Height) };
    }
    // Desktop physical coordinates map only the actual overlap. Calibration is around its center.
    public float SourcePosition(float desktopAlong, float offset, float scale)
    {
        float p = (desktopAlong-(Start+End)*.5f)/scale+(Start+End)*.5f+offset;
        return (p-(Horizontal ? Source.Left : Source.Top))/(Horizontal ? Source.Width : Source.Height);
    }
}
public static class Geometry
{
    public static Seam? Join(Rectangle s, Rectangle t, int tolerance=16)
    {
        if(s.IntersectsWith(t))return null;
        int x0=Math.Max(s.Left,t.Left), x1=Math.Min(s.Right,t.Right);
        int y0=Math.Max(s.Top,t.Top), y1=Math.Min(s.Bottom,t.Bottom);
        if(x1>x0 && Math.Abs(s.Top-t.Bottom)<=tolerance) return new(s,t,Edge.Top,x0,x1);
        if(x1>x0 && Math.Abs(s.Bottom-t.Top)<=tolerance) return new(s,t,Edge.Bottom,x0,x1);
        if(y1>y0 && Math.Abs(s.Left-t.Right)<=tolerance) return new(s,t,Edge.Left,y0,y1);
        if(y1>y0 && Math.Abs(s.Right-t.Left)<=tolerance) return new(s,t,Edge.Right,y0,y1);
        return null;
    }
    public static float Fade(float x) { x=Math.Clamp(x,0,1); float y=1-x; return y*y*y*(1+3*x); }
    public static float Blend(float dt,float milliseconds) => milliseconds<=0?1:1-MathF.Exp(-Math.Max(0,dt)/(milliseconds/1000));
    public static float Distance(Edge edge,float x,float y) => edge switch { Edge.Top=>y,Edge.Bottom=>1-y,Edge.Left=>x,_=>1-x };
    public static Vector2 RotateUV(Vector2 p,int rotation) => rotation switch { 2=>new(p.Y,1-p.X),3=>new(1-p.X,1-p.Y),4=>new(1-p.Y,p.X),_=>p };
    public static Vector4 Premultiply(Vector3 c,float opacity,float fade)
    {
        c=Vector3.Clamp(c,Vector3.Zero,Vector3.One);
        float a=Math.Clamp(Math.Max(c.X,Math.Max(c.Y,c.Z))*opacity*fade,0,1);
        return new(c*a,a);
    }
}
