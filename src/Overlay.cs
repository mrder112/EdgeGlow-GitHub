using System.ComponentModel;
using System.Numerics;
namespace EdgeGlow;
internal sealed class Overlay : Form
{
    internal readonly Seam Seam;
    internal readonly string TargetId;
    internal bool CaptureExcluded {get;private set;}
    private readonly nint memory,hbitmap,oldBitmap,bits;
    private readonly Vector3[] smooth,blurred,previous;
    private readonly ColorLine colors;
    private readonly float[] fades,lateral,positions,kernel;
    private readonly Rectangle band;
    private bool shown,first=true,resourcesDisposed;
    internal long SubmittedFrames {get;private set;}
    public Overlay(Seam seam,string id,Settings settings,string? sourceId=null)
    {
        Seam=seam;TargetId=id;band=seam.Band(settings.Depth);
        smooth=new Vector3[settings.Samples];blurred=new Vector3[settings.Samples];previous=new Vector3[settings.Samples];
        AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;Bounds=band;
        // Separable SIMD rasterization into a persistent premultiplied DIB.
        int along=seam.Horizontal?band.Width:band.Height,deep=seam.Horizontal?band.Height:band.Width;
        var info=new Native.BitmapInfo{Size=40,Width=band.Width,Height=-band.Height,Planes=1,BitCount=32};
        memory=Native.CreateCompatibleDC(0);hbitmap=Native.CreateDIBSection(memory,ref info,0,out bits,0,0);
        if(memory==0||hbitmap==0){if(hbitmap!=0)Native.DeleteObject(hbitmap);if(memory!=0)Native.DeleteDC(memory);throw new Win32Exception();}
        oldBitmap=Native.SelectObject(memory,hbitmap);
        colors=new ColorLine(along);lateral=new float[along];positions=new float[along];fades=new float[deep];
        var c=settings.Calibration(id,sourceId);
        for(int i=0;i<along;i++){
            float desktop=(seam.Horizontal?band.Left:band.Top)+(i+.5f)/along*(seam.Horizontal?band.Width:band.Height);
            float u=seam.SourcePosition(desktop,c.Offset,c.Scale);positions[i]=Math.Clamp(u*(settings.Samples-1),0,settings.Samples-1);
            if(desktop<seam.Start||desktop>=seam.End||u<0||u>1)continue;
            float feather=Math.Min(40,(seam.End-seam.Start)*.05f);
            lateral[i]=1-Geometry.Fade(Math.Min(desktop-seam.Start,seam.End-desktop)/Math.Max(1,feather));
        }
        for(int j=0;j<deep;j++){float t=j/(float)Math.Max(1,deep-1);fades[j]=Geometry.Fade(seam.TargetEdge is Edge.Bottom or Edge.Right?1-t:t);}
        int radius=settings.EffectiveBlur<=0?0:Math.Clamp((int)(settings.EffectiveBlur*settings.Samples),1,64);float sigma=Math.Max(1,radius*.5f);kernel=new float[radius*2+1];float sum=0;
        for(int j=-radius;j<=radius;j++){float w=MathF.Exp(-.5f*j*j/(sigma*sigma));kernel[j+radius]=w;sum+=w;}
        for(int j=0;j<kernel.Length;j++)kernel[j]/=sum;
        _=Handle;CaptureExcluded=Native.SetWindowDisplayAffinity(Handle,0x11);
    }
    protected override bool ShowWithoutActivation=>true;
    protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x80000|0x20|0x80|0x08000000;return p;}}
    protected override void WndProc(ref Message m){if(m.Msg==0x84){m.Result=-1;return;}if(m.Msg==0x21){m.Result=3;return;}base.WndProc(ref m);}
    internal void SetVisible(bool visible){if(!visible){if(shown)Hide();shown=false;return;}if(!shown){Show();Native.SetWindowPos(Handle,new nint(-1),band.X,band.Y,band.Width,band.Height,0x10|0x40);shown=true;}}
    internal unsafe void Render(Vector3[] strip,Settings s,float dt)
    {
        bool newColors=first||!strip.AsSpan().SequenceEqual(previous);
        if(newColors){
            strip.CopyTo(previous,0);int radius=kernel.Length/2;
            for(int i=0;i<smooth.Length;i++){Vector3 sum=default;for(int j=-radius;j<=radius;j++)sum+=strip[Math.Clamp(i+j,0,smooth.Length-1)]*kernel[j+radius];blurred[i]=sum;}
        }
        float factor=Geometry.Blend(dt,s.EffectiveSmoothing),change=0;
        for(int i=0;i<smooth.Length;i++){var next=Vector3.Lerp(smooth[i],blurred[i],factor);if(Vector3.DistanceSquared(next,blurred[i])<.0000001f)next=blurred[i];change=Math.Max(change,Vector3.DistanceSquared(smooth[i],next));smooth[i]=next;}
        if(!first&&!newColors&&change==0)return; // settled static image: keep the existing layer.
        first=false;
        for(int i=0;i<positions.Length;i++){
            float p=positions[i];int k=(int)p;var c=s.NoSmoothing?smooth[Math.Clamp((int)MathF.Round(p),0,smooth.Length-1)]:Vector3.Lerp(smooth[k],smooth[Math.Min(smooth.Length-1,k+1)],p-k);float gray=Vector3.Dot(c,new(.2126f,.7152f,.0722f));
            c=Vector3.Clamp((new Vector3(gray)+(c-new Vector3(gray))*s.Saturation)*s.Brightness,Vector3.Zero,Vector3.One);
            colors.Set(i,Geometry.Premultiply(c,s.Opacity,lateral[i]));
        }
        Raster.Fill(new Span<uint>((void*)bits,band.Width*band.Height),band.Width,band.Height,colors,fades,Seam.Horizontal);
        var pos=new Native.Point(band.X,band.Y);var size=new Native.Size(band.Width,band.Height);var zero=new Native.Point();var blend=new Native.Blend{Alpha=255,Format=1};
        if(!Native.UpdateLayeredWindow(Handle,0,ref pos,ref size,memory,ref zero,0,ref blend,2))throw new Win32Exception();SubmittedFrames++;
    }
    protected override void Dispose(bool disposing){if(disposing&&!resourcesDisposed){resourcesDisposed=true;Native.SelectObject(memory,oldBitmap);Native.DeleteObject(hbitmap);Native.DeleteDC(memory);}base.Dispose(disposing);}
}
