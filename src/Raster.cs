using System.Numerics;
namespace EdgeGlow;
internal sealed class ColorLine(int length)
{
    internal readonly float[] B=new float[length],G=new float[length],R=new float[length],A=new float[length];
    internal void Set(int i,Vector4 value){B[i]=value.Z*255;G[i]=value.Y*255;R[i]=value.X*255;A[i]=value.W*255;}
}
internal static class Raster
{
    private static readonly byte[] Bayer=[0,48,12,60,3,51,15,63,32,16,44,28,35,19,47,31,8,56,4,52,11,59,7,55,40,24,36,20,43,27,39,23,2,50,14,62,1,49,13,61,34,18,46,30,33,17,45,29,10,58,6,54,9,57,5,53,42,26,38,22,41,25,37,21];
    internal static float Threshold(int x,int y)=>(Bayer[(y&7)*8+(x&7)]+.5f)/64f;
    private static readonly Vector<float>[,] Dithers=BuildDithers();
    private static Vector<float>[,] BuildDithers(){var result=new Vector<float>[8,8];for(int y=0;y<8;y++)for(int x=0;x<8;x++){var values=new float[Vector<float>.Count];for(int i=0;i<values.Length;i++)values[i]=Threshold(x+i,y);result[y,x]=new(values);}return result;}
    // Full-precision premultiplied channels survive until the final 8-bit conversion.
    // An identical, screen-stationary threshold for all channels preserves RGB <= alpha.
    private static Vector<uint> Quantize(Vector<float> c,Vector<float> f,Vector<float> d)=>Vector.ConvertToUInt32(Vector.Min(new Vector<float>(255),Vector.Max(Vector<float>.Zero,c*f+d)));
    private static Vector<uint> Pack(Vector<float> b,Vector<float> g,Vector<float> r,Vector<float> a,Vector<float> f,Vector<float> d)=>Quantize(b,f,d)|(Quantize(g,f,d)<<8)|(Quantize(r,f,d)<<16)|(Quantize(a,f,d)<<24);
    internal static void Fill(Span<uint> pixels,int width,int height,ColorLine colors,ReadOnlySpan<float> fades,bool horizontal)
    {
        for(int y=0;y<height;y++){
            var row=pixels.Slice(y*width,width);int x=0;
            if(horizontal){var f=new Vector<float>(fades[y]);for(;x<=width-Vector<float>.Count;x+=Vector<float>.Count)Pack(new(colors.B.AsSpan(x)),new(colors.G.AsSpan(x)),new(colors.R.AsSpan(x)),new(colors.A.AsSpan(x)),f,Dithers[y&7,x&7]).CopyTo(row.Slice(x));}
            else{var b=new Vector<float>(colors.B[y]);var g=new Vector<float>(colors.G[y]);var r=new Vector<float>(colors.R[y]);var a=new Vector<float>(colors.A[y]);for(;x<=width-Vector<float>.Count;x+=Vector<float>.Count)Pack(b,g,r,a,new Vector<float>(fades.Slice(x)),Dithers[y&7,x&7]).CopyTo(row.Slice(x));}
            for(;x<width;x++){int i=horizontal?x:y;float f=fades[horizontal?y:x],d=Threshold(x,y);row[x]=(uint)Math.Clamp(colors.B[i]*f+d,0,255)|((uint)Math.Clamp(colors.G[i]*f+d,0,255)<<8)|((uint)Math.Clamp(colors.R[i]*f+d,0,255)<<16)|((uint)Math.Clamp(colors.A[i]*f+d,0,255)<<24);}
        }
    }
}
