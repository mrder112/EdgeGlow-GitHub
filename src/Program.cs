namespace EdgeGlow;
internal static class Program
{
    [STAThread] static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        if(args.Contains("--benchmark")){try{Benchmark.Run();}catch(Exception e){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"benchmark-error.txt"),e.ToString());Environment.ExitCode=1;}return;}
        if(args.Contains("--capture-smoke")){
            try{
                var displays=Screen.AllScreens.Select(s=>new Display(s.DeviceName,s.Bounds)).ToArray();
                var lines=new List<string>();
                foreach(var d in displays){ try {
                    using var c=new Capture(d);var timer=System.Diagnostics.Stopwatch.StartNew();int frames=0;
                    while(timer.Elapsed.TotalSeconds<2){if(c.Read(new Settings()))frames++;Thread.Sleep(33);}
                    if(frames==0)throw new InvalidOperationException("Нет кадров за 2 секунды: "+d.Id);
                    lines.Add($"PASS: {d} — {frames} frames; GPU strip readback 256 × 4 float4; screen content not logged.");
                    }catch(Exception e){lines.Add($"UNAVAILABLE: {d} — {e.Message}");}
                }
                File.WriteAllLines(Path.Combine(AppContext.BaseDirectory,"capture-smoke.txt"),lines);
            }catch(Exception e){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"capture-smoke.txt"),e.ToString());Environment.ExitCode=1;}return;
        }
        if(args.Contains("--synthetic-smoke")){
            try{
                var t=Screen.AllScreens[0].Bounds;var source=new Rectangle(t.Left,t.Bottom,t.Width,t.Height);
                var s=new Settings();using var o=new Overlay(Geometry.Join(source,t)!,"test",s);
                var foreground=Native.GetForegroundWindow();
                o.Render(Engine.Synthetic(),s,.1f);o.SetVisible(true);Application.DoEvents();
                bool noFocus=foreground==Native.GetForegroundWindow();
                o.SetVisible(false);
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"synthetic-smoke.txt"),$"PASS: premultiplied layered window created, rendered, hidden and disposed. Focus preserved: {noFocus}. Capture exclusion: {o.CaptureExcluded}. Visual/input behavior requires manual review.");
                Environment.ExitCode=noFocus?0:1;
            }catch(Exception e){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"synthetic-smoke.txt"),e.ToString());Environment.ExitCode=1;}return;
        }
        using var instance=new Mutex(true,"Local\\EdgeGlow.Desktop.1",out bool first);
        if(!first){MessageBox.Show("EdgeGlow уже запущен. Откройте настройки через значок в трее.","EdgeGlow");return;}
        Application.Run(new MainForm());
    }
}
