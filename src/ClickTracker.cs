using System.ComponentModel;
using System.Runtime.InteropServices;
namespace EdgeGlow;
// Observes button-down location only. Never suppresses input, never logs clicks or screen content.
internal sealed class ClickTracker:IDisposable
{
    private readonly Native.HookProc callback;
    private readonly nint hook;
    internal Point? LastClick {get;private set;}
    internal ClickTracker(){callback=OnMouse;hook=Native.SetWindowsHookEx(14,callback,0,0);if(hook==0)throw new Win32Exception();}
    private nint OnMouse(int code,nint message,nint data)
    {
        if(code>=0&&(message==0x201||message==0x204||message==0x207)){
            var m=Marshal.PtrToStructure<Native.MouseHookData>(data);
            var window=Native.WindowFromPoint(m.Point);Native.GetWindowThreadProcessId(window,out uint pid);
            if(pid!=Environment.ProcessId||(Native.GetWindowLongPtr(window,-20).ToInt64()&0x08000000)!=0)LastClick=new Point(m.Point.X,m.Point.Y);
        }
        return Native.CallNextHookEx(hook,code,message,data);
    }
    internal Point? Take(){var p=LastClick;LastClick=null;return p;}
    public void Dispose(){Native.UnhookWindowsHookEx(hook);GC.KeepAlive(callback);}
}
