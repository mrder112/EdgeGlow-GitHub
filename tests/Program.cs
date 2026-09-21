using EdgeGlow;
using System.Numerics;
int count=0;
void Check(bool ok,string name){count++;if(!ok)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);}
void Near(float a,float b,string name)=>Check(Math.Abs(a-b)<.0001,name);
var source=new Rectangle(-1920,0,1920,1080);
var upper=new Rectangle(-1920,-1440,2560,1440);
var seam=Geometry.Join(source,upper)!;
Check(seam.SourceEdge==Edge.Top&&seam.TargetEdge==Edge.Bottom,"negative coordinates: lower source → upper target");
Check(seam.Start==-1920&&seam.End==0,"partial overlap, unequal resolution");
Near(seam.SourcePosition(-1920,0,1),0,"red stays on left");Near(seam.SourcePosition(0,0,1),1,"blue stays on right");
Near(seam.SourcePosition(-960,192,1),.6f,"manual offset");Near(seam.SourcePosition(-480,0,2),.625f,"manual scale around seam center");
var band=seam.Band(.35f);Check(band.Bottom==upper.Bottom&&band.Height==504,"depth follows receiver dimensions");
foreach(var pair in new[]{(new Rectangle(-1920,1080,1920,1080),Edge.Bottom,Edge.Top),(new Rectangle(-3120,0,1200,1920),Edge.Left,Edge.Right),(new Rectangle(0,200,1080,1920),Edge.Right,Edge.Left)}){var j=Geometry.Join(source,pair.Item1)!;Check(j.SourceEdge==pair.Item2&&j.TargetEdge==pair.Item3,"join "+pair.Item2);}
Check(Geometry.Join(source,new Rectangle(3000,0,1920,1080))==null,"disconnected displays rejected");
Check(Geometry.Join(source,new Rectangle(0,1080,100,100))==null,"corner touching has no seam");
Check(Geometry.Join(source,source)==null,"source cannot receive itself");
foreach(var edge in Enum.GetValues<Edge>()){
    float x=edge==Edge.Right?1:0,y=edge==Edge.Bottom?1:0;
    Near(Geometry.Distance(edge,x,y),0,"direction starts at "+edge);
    Near(Geometry.Distance(edge,1-x,1-y),1,"direction ends at "+edge);
}
Near(Geometry.Fade(0),1,"fade at seam");Near(Geometry.Fade(1),0,"fade at depth");Near(Geometry.Fade(3),0,"fade beyond depth");
float last=1;for(int i=0;i<=1000;i++){float v=Geometry.Fade(i/1000f);if(v>last+.000001f||v<0||v>1)throw new Exception("fade nonmonotone");last=v;}Check(true,"fade monotonically reaches zero");
Check(Math.Abs((Geometry.Fade(.999f)-Geometry.Fade(1))/.001f)<.0001,"smooth far boundary derivative");
float a15=0,a60=0;for(int i=0;i<15;i++)a15+=(1-a15)*Geometry.Blend(1/15f,180);for(int i=0;i<60;i++)a60+=(1-a60)*Geometry.Blend(1/60f,180);Near(a15,a60,"time smoothing independent of frame rate");
Near(Geometry.Blend(.02f,0),1,"zero smoothing immediate");
Check(Geometry.Premultiply(Vector3.Zero,.8f,1)==Vector4.Zero,"black source has zero alpha");
var red=Geometry.Premultiply(new(1,0,0),.5f,1);Near(red.X,.5f,"premultiplied red");Near(red.W,.5f,"premultiplied alpha");
Check(Geometry.Premultiply(Vector3.One,.8f,0)==Vector4.Zero,"outside glow fully transparent");
for(int rotation=1;rotation<=4;rotation++){var uv=Geometry.RotateUV(new(.2f,.7f),rotation);Check(uv.X>=0&&uv.X<=1&&uv.Y>=0&&uv.Y<=1,"rotation bounds "+rotation);}
Check(Geometry.RotateUV(new(.2f,.7f),2)==new Vector2(.7f,.8f),"portrait rotation 90");
var settings=new Settings{Brightness=float.NaN,Depth=-2,Fps=99,Targets=null!,Calibrations=null!};settings.Validate();Check(settings.Brightness==1&&settings.Depth==.05f&&settings.Fps==30&&settings.Targets!=null,"invalid settings safely normalized");
using(var icon=AppBrand.CreateIcon())Check(icon.Width>=16&&icon.Height>=16,"embedded application icon loads");
var random=new Random(44);foreach(bool horizontal in new[]{true,false}){
    const int width=37,height=19;var colors=new ColorLine(horizontal?width:height);var fades=new float[horizontal?height:width];var rendered=new uint[width*height];
    for(int i=0;i<colors.A.Length;i++){float a=(float)random.NextDouble();colors.Set(i,new Vector4((float)random.NextDouble()*a,(float)random.NextDouble()*a,(float)random.NextDouble()*a,a));}for(int i=0;i<fades.Length;i++)fades[i]=(float)random.NextDouble();
    Raster.Fill(rendered,width,height,colors,fades,horizontal);
    bool matches=true,premultiplied=true;for(int y=0;y<height;y++)for(int x=0;x<width;x++){int i=horizontal?x:y;float f=fades[horizontal?y:x],d=Raster.Threshold(x,y);uint reference=(uint)(colors.B[i]*f+d)|((uint)(colors.G[i]*f+d)<<8)|((uint)(colors.R[i]*f+d)<<16)|((uint)(colors.A[i]*f+d)<<24);uint p=rendered[y*width+x];matches&=p==reference;premultiplied&=(p&255)<=(p>>24)&&((p>>8)&255)<=(p>>24)&&((p>>16)&255)<=(p>>24);}
    Check(matches,"dither SIMD matches scalar arithmetic including tail: "+horizontal);Check(premultiplied,"dither preserves premultiplied alpha: "+horizontal);
    Array.Fill(fades,0);Raster.Fill(rendered,width,height,colors,fades,horizontal);Check(rendered.All(p=>p==0),"dither leaves zero fade fully transparent: "+horizontal);
}
var flat=new ColorLine(8);for(int i=0;i<8;i++)flat.Set(i,new Vector4(.001f,.001f,.001f,.001f));var fine=new uint[64];Raster.Fill(fine,8,8,flat,Enumerable.Repeat(1f,8).ToArray(),true);
Check(fine.Any(p=>p==0)&&fine.Any(p=>p!=0),"sub-byte brightness survives as spatial coverage instead of a hard band");
Check(Math.Abs(fine.Average(p=>(double)(p>>24))-.255)<1.0/64,"ordered dither preserves mean alpha within one tile sample");
var repeated=new uint[64];Raster.Fill(repeated,8,8,flat,Enumerable.Repeat(1f,8).ToArray(),true);Check(fine.SequenceEqual(repeated),"dither is stationary across frames");
var disabled=new Settings{NoSmoothing=true,Smoothing=500,Blur=.2f};disabled.Validate();Check(disabled.EffectiveBlur==0&&disabled.EffectiveSmoothing==0,"disable smoothing bypasses spatial and temporal filtering");
Check(Geometry.Blend(.001f,disabled.EffectiveSmoothing)==1,"disabled smoothing changes colors immediately");
disabled.NoSmoothing=false;Check(disabled.EffectiveBlur==.2f&&disabled.EffectiveSmoothing==500,"re-enabling smoothing preserves previous strengths");
Check(new Settings().CaptureDuringMove,"capture during window motion enabled by default");
var roundtrip=System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(new Settings{CaptureDuringMove=false,NoSmoothing=true,MoveHotKey=(int)Keys.F8}))!;Check(!roundtrip.CaptureDuringMove&&roundtrip.NoSmoothing&&roundtrip.MoveHotKey==(int)Keys.F8,"new options and motion hotkey persist");
var sparse=new Settings{CaptureFps=5,Fps=30,Smoothing=400};sparse.Validate();Check(sparse.CaptureFps==5&&sparse.Fps==30,"eco capture and interpolation rates independent");
if(args.Contains("--gpu")) {
    using var gpu=new Capture();var pixels=new byte[256*256*4];
    for(int y=0;y<256;y++)for(int x=0;x<256;x++){int i=(y*256+x)*4;pixels[i]=(byte)x;pixels[i+2]=(byte)(255-x);pixels[i+3]=255;}
    var colors=gpu.ProcessSynthetic(pixels);
    Check(colors[0][0].X>.98f&&colors[0][255].Z>.98f,"GPU top edge retains red-left blue-right");
    Check(colors[1][0].X>.98f&&colors[1][255].Z>.98f,"GPU bottom edge orientation");
    Check(colors[2].All(c=>c.X>.97f)&&colors[3].All(c=>c.Z>.97f),"GPU opposing side strips");
    colors=gpu.ProcessSynthetic(new byte[pixels.Length]);Check(colors.All(row=>row.All(c=>c.Length()<.00001)),"GPU black frame becomes zero RGB");
    colors=gpu.ProcessSynthetic(pixels,settings:new Settings{CropLeft=128});Check(colors[0][0].X<.51f&&colors[0][0].Z>.49f,"GPU manual crop");
    // Rotate the known input signal into each DXGI surface orientation.
    for(int r=2;r<=4;r++){
        var rotated=new byte[pixels.Length];
        for(int y=0;y<256;y++)for(int x=0;x<256;x++){
            var p=Geometry.RotateUV(new((x+.5f)/256,(y+.5f)/256),r);int j=((int)(p.Y*256)*256+(int)(p.X*256))*4;rotated[j]=(byte)x;rotated[j+2]=(byte)(255-x);rotated[j+3]=255;
        }
        colors=gpu.ProcessSynthetic(rotated,r);Check(colors[0][0].X>.98f&&colors[0][255].Z>.98f,"GPU oriented synthetic input "+r);
    }
    foreach(int n in new[]{32,64,128}){using var reduced=new Capture(n);var output=reduced.ProcessSynthetic(pixels);Check(output.Length==4&&output[0].Length==n&&output[0][0].X>.97f&&output[0][n-1].Z>.97f,"GPU reduced sample density "+n);}
}
Console.WriteLine($"{count} checks passed"+(args.Contains("--gpu")?" (GPU included).":" (CPU only)."));
