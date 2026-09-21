using System.Text.Json;
namespace EdgeGlow;
public sealed class Calibration { public float Offset {get;set;} public float Scale {get;set;}=1; }
public sealed class Settings
{
    public string Source {get;set;}="";
    public List<string> Targets {get;set;}=[];
    public bool Follow {get;set;}
    public bool TrackClicks {get;set;}
    public bool AboveWindows {get;set;}
    public bool CaptureDuringMove {get;set;}=true;
    public bool NoSmoothing {get;set;}
    [System.Text.Json.Serialization.JsonIgnore] public float EffectiveSmoothing => NoSmoothing?0:Smoothing;
    [System.Text.Json.Serialization.JsonIgnore] public float EffectiveBlur => NoSmoothing?0:Blur;
    public float Brightness {get;set;}=1;
    public float Opacity {get;set;}=.55f;
    public float Saturation {get;set;}=1.15f;
    public float Depth {get;set;}=.35f;
    public float Blur {get;set;}=.06f;
    public float Smoothing {get;set;}=180;
    public float Strip {get;set;}=.03f;
    public int Fps {get;set;}=30;
    public int CaptureFps {get;set;}=30;
    public int Samples {get;set;}=128;
    public int CropLeft {get;set;}
    public int CropTop {get;set;}
    public int CropRight {get;set;}
    public int CropBottom {get;set;}
    public int HotKey {get;set;}=(int)Keys.F10;
    public uint HotModifiers {get;set;}=6;
    public int MoveHotKey {get;set;}=(int)Keys.F9;
    public uint MoveHotModifiers {get;set;}=6;
    public Dictionary<string,Calibration> Calibrations {get;set;}=[];
    public static string DirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"EdgeGlow");
    public static string FilePath => Path.Combine(DirectoryPath,"settings.json");
    public void Validate()
    {
        static float C(float v,float min,float max,float def)=>float.IsFinite(v)?Math.Clamp(v,min,max):def;
        Source??=""; Targets??=[]; Calibrations??=[];
        Brightness=C(Brightness,0,3,1); Opacity=C(Opacity,0,1,.55f); Saturation=C(Saturation,0,3,1.15f);
        Depth=C(Depth,.05f,1,.35f); Blur=C(Blur,0,.25f,.06f); Smoothing=C(Smoothing,0,2000,180); Strip=C(Strip,.005f,.25f,.03f);
        if(Fps is not (15 or 30 or 60)) Fps=30;
        if(CaptureFps is not (1 or 2 or 5 or 10 or 15 or 30 or 60)) CaptureFps=30;
        if(Samples is not (32 or 64 or 128 or 256)) Samples=128;
        CropLeft=Math.Clamp(CropLeft,0,10000); CropRight=Math.Clamp(CropRight,0,10000); CropTop=Math.Clamp(CropTop,0,10000); CropBottom=Math.Clamp(CropBottom,0,10000);
        if(HotKey<32||HotKey>254) HotKey=(int)Keys.F10; HotModifiers&=15;
        if(MoveHotKey<32||MoveHotKey>254)MoveHotKey=(int)Keys.F9;MoveHotModifiers&=15;
        foreach(var key in Calibrations.Keys.ToArray()) { var c=Calibrations[key]; if(c==null){Calibrations.Remove(key);continue;} c.Offset=C(c.Offset,-10000,10000,0); c.Scale=C(c.Scale,.25f,4,1); }
    }
    public static Settings Load()
    {
        try { var s=JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath))??new(); s.Validate(); return s; }
        catch(Exception e) when(e is IOException or JsonException or UnauthorizedAccessException) { return new(); }
    }
    public void Save() { Validate(); Directory.CreateDirectory(DirectoryPath); File.WriteAllText(FilePath+".tmp",JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true})); File.Move(FilePath+".tmp",FilePath,true); }
    public Calibration Calibration(string target,string? actualSource=null) => Calibrations.TryGetValue((actualSource??Source)+"|"+target,out var c)?c:new();
}
