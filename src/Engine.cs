using System.Diagnostics;
namespace EdgeGlow;
internal sealed class Engine : IDisposable
{
    private readonly Settings settings;
    private readonly List<Overlay> overlays=[];
    private Capture? capture;
    private ClickTracker? clicks;
    private InteractionGuard? interaction;
    private readonly System.Windows.Forms.Timer timer=new(){Interval=15};
    private readonly Stopwatch clock=Stopwatch.StartNew();
    private double lastFrame,nextFrame,lastCapture=-100,lastRetry,candidateSince,lastTopology;
    private System.Numerics.Vector3[] syntheticColors=[];
    private string? candidate,current;
    private bool test, suspended;
    internal bool Running {get;private set;}
    internal string Status {get;private set;}="Приостановлено";
    internal event Action? Changed;
    internal List<Display> Displays {get;private set;}=[];
    private string signature="";
    internal Engine(Settings settings){this.settings=settings; RefreshDisplays();timer.Tick+=Tick;timer.Start();}
    internal void RefreshDisplays(){Displays=Screen.AllScreens.Select(s=>new Display(s.DeviceName,s.Bounds)).ToList();signature=string.Join(";",Displays);}
    internal void Start(bool synthetic=false)
    {
        Stop();test=synthetic;syntheticColors=Synthetic(settings.Samples);Running=true;current=settings.Source;lastFrame=clock.Elapsed.TotalSeconds;nextFrame=lastFrame;lastCapture=-100;
        try{interaction=new InteractionGuard(()=>{if(!settings.CaptureDuringMove)foreach(var o in overlays)o.SetVisible(false);});}catch(Exception e){Running=false;Status="Не удалось включить защиту перемещения окон: "+e.Message;Changed?.Invoke();return;}
        if(settings.TrackClicks)try{clicks=new ClickTracker();}catch(Exception e){Running=false;Status="Не удалось включить отслеживание щелчков: "+e.Message;Changed?.Invoke();return;}
        TryBuild();Changed?.Invoke();
    }
    internal void Stop(){Running=false;clicks?.Dispose();clicks=null;interaction?.Dispose();interaction=null;Release();Status="Приостановлено";Changed?.Invoke();}
    private void Release(){foreach(var o in overlays)o.Dispose();overlays.Clear();capture?.Dispose();capture=null;}
    private void TryBuild()
    {
        Release();Native.DwmFlush();lastRetry=clock.Elapsed.TotalSeconds;
        if(suspended)return;
        try {
            var source=Displays.FirstOrDefault(d=>d.Id==current)??throw new InvalidOperationException("Выберите доступный монитор-источник.");
            var pairs=Displays.Where(d=>(settings.Targets.Contains(d.Id)||((settings.Follow||settings.TrackClicks)&&d.Id==settings.Source))&&d.Id!=source.Id).Select(d=>(d,seam:Geometry.Join(source.Bounds,d.Bounds))).Where(p=>p.seam!=null).ToArray();
            if(pairs.Length==0)throw new InvalidOperationException("Нет выбранного соседнего получателя. Проверьте схему экранов Windows.");
            // Old windows are destroyed BEFORE switching the capture source. Never draw onto the source.
            if(!test)capture=new Capture(source,settings.Samples);
            foreach(var (target,seam) in pairs)overlays.Add(new Overlay(seam!,target.Id,settings,source.Id));
            Status=test?"Тестовое свечение · без захвата":"Захват SDR · "+source.Id;
            if(overlays.Any(o=>!o.CaptureExcluded))Status+=" · Windows не подтвердила исключение из захвата; источник отделён геометрически";
        }catch(Exception e){Release();Status="Ожидание: "+e.Message;}
        Changed?.Invoke();
    }
    internal void Suspend(bool state){suspended=state;Release();if(!state&&Running){RefreshDisplays();TryBuild();}else if(state){Status="Пауза: сеанс заблокирован или компьютер засыпает";Changed?.Invoke();}}
    internal void ConfigurationChanged(){RefreshDisplays();if(Running)TryBuild();Changed?.Invoke();}
    private void Tick(object? sender,EventArgs args)
    {
        double now=clock.Elapsed.TotalSeconds;
        if(now-lastTopology>2){lastTopology=now;var before=signature;RefreshDisplays();if(before!=signature){if(Running)TryBuild();Changed?.Invoke();}}
        if(!Running||suspended)return;
        if(!settings.CaptureDuringMove&&interaction?.Paused==true){foreach(var o in overlays)o.SetVisible(false);return;}
        if(clicks?.Take() is Point point){var clicked=Displays.FirstOrDefault(d=>d.Bounds.Contains(point))?.Id;if(clicked!=null&&clicked!=current){current=clicked;candidate=null;lastCapture=-100;TryBuild();}}
        if(settings.Follow&&!test){var next=Native.ActiveDisplay(Displays);if(next==null){candidate=null;}else if(next!=current){if(candidate!=next){candidate=next;candidateSince=now;}else if(now-candidateSince>=.5){current=next;candidate=null;TryBuild();}}else candidate=null;}
        if(overlays.Count==0){if(now-lastRetry>2)TryBuild();return;}
        if(now<nextFrame)return;
        nextFrame+=1.0/settings.Fps;if(nextFrame<now)nextFrame=now+1.0/settings.Fps;
        float dt=(float)Math.Min(.25,now-lastFrame);lastFrame=now;
        try {
            var blocked=settings.AboveWindows?[]:Native.Obstacles(overlays.Select(o=>o.Handle).ToHashSet());
            var visible=overlays.Where(o=>!blocked.Any(r=>r.IntersectsWith(o.Seam.Band(settings.Depth)))).ToHashSet();
            // Hidden recipients require no capture; interpolation uses its own update frequency.
            if(visible.Count>0&&now-lastCapture>=1.0/settings.CaptureFps){capture?.Read(settings);lastCapture=now;}
            foreach(var o in overlays){
                if(!visible.Contains(o)){o.SetVisible(false);continue;}
                var colors=test?syntheticColors:capture!.Colors[(int)o.Seam.SourceEdge];
                o.Render(colors,settings,dt);o.SetVisible(true);
            }
        }catch(Exception e){Release();lastRetry=now;Status="Восстановление захвата: "+e.Message;Changed?.Invoke();}
    }
    internal static System.Numerics.Vector3[] Synthetic(int samples=128)=>Enumerable.Range(0,samples).Select(i=>new System.Numerics.Vector3(1-i/(float)(samples-1),.08f,i/(float)(samples-1))).ToArray();
    public void Dispose(){timer.Stop();timer.Dispose();clicks?.Dispose();interaction?.Dispose();Release();}
}
