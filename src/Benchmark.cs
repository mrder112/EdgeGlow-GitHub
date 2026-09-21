using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
namespace EdgeGlow;
internal static class Benchmark
{
    internal static void Run()
    {
        var config=Settings.Load();var screens=Screen.AllScreens.Select(x=>new Display(x.DeviceName,x.Bounds)).ToArray();
        var source=screens.First(x=>x.Id==config.Source);var target=screens.First(x=>config.Targets.Contains(x.Id)&&x.Id!=source.Id&&Geometry.Join(source.Bounds,x.Bounds)!=null);
        var report=new List<object>();
        foreach(var mode in new[]{(Capture:15,Render:30),(Capture:30,Render:30),(Capture:60,Render:60),(Capture:5,Render:30)}){
            config.CaptureFps=mode.Capture;config.Fps=mode.Render;config.Smoothing=mode.Capture==5?400:180;
            config.Samples=mode.Capture==5?64:128;
            using var capture=new Capture(source,config.Samples);using var overlay=new Overlay(Geometry.Join(source.Bounds,target.Bounds)!,target.Id,config,source.Id);
            var colors=new Vector3[config.Samples];var clock=Stopwatch.StartNew();double nextCapture=0,nextRender=0,lastRender=0,captureMs=0,renderMs=0;int frames=0,captures=0;
            using var process=Process.GetCurrentProcess();double cpu=process.TotalProcessorTime.TotalSeconds;
            while(clock.Elapsed.TotalSeconds<8){
                double now=clock.Elapsed.TotalSeconds;
                if(now>=nextCapture){var t=Stopwatch.StartNew();capture.Read(config);captureMs+=t.Elapsed.TotalMilliseconds;captures++;nextCapture=now+1.0/mode.Capture;
                    // Deliberately changing synthetic colors prevent static-scene skipping.
                    for(int i=0;i<colors.Length;i++)colors[i]=new((MathF.Sin((float)now*3+i*.025f)+1)*.5f,.15f,(MathF.Cos((float)now*2+i*.02f)+1)*.5f);
                }
                if(now>=nextRender){var t=Stopwatch.StartNew();overlay.Render(colors,config,(float)(now-lastRender));renderMs+=t.Elapsed.TotalMilliseconds;lastRender=now;nextRender=now+1.0/mode.Render;frames++;}
                Application.DoEvents();Thread.Sleep(1);
            }
            process.Refresh();double elapsed=clock.Elapsed.TotalSeconds,cpuDelta=process.TotalProcessorTime.TotalSeconds-cpu;
            report.Add(new {captureLimit=mode.Capture,renderLimit=mode.Render,samplesPerEdge=config.Samples,seconds=elapsed,captureAttempts=captures,renderFrames=frames,actualRenderFps=frames/elapsed,cpuPercentTotal=100*cpuDelta/elapsed/Environment.ProcessorCount,cpuPercentOneCore=100*cpuDelta/elapsed,meanCaptureMs=captureMs/captures,meanRenderMs=renderMs/frames,workingSetMB=process.WorkingSet64/1048576.0,privateMB=process.PrivateMemorySize64/1048576.0,handles=process.HandleCount});
        }
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"benchmark.json"),JsonSerializer.Serialize(new{description="Real desktop capture + continuously changing synthetic glow; hidden layered surface, no screen pixels logged",logicalProcessors=Environment.ProcessorCount,source=source.ToString(),target=target.ToString(),modes=report},new JsonSerializerOptions{WriteIndented=true}));
    }
}
