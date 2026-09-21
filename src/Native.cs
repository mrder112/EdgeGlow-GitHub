using System.Runtime.InteropServices;
using System.Text;
namespace EdgeGlow;
internal static class Native
{
    [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X,Y; public Point(int x,int y){X=x;Y=y;} }
    [StructLayout(LayoutKind.Sequential)] internal struct Size { public int X,Y; public Size(int x,int y){X=x;Y=y;} }
    [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int Left,Top,Right,Bottom; public readonly Rectangle Rectangle=>Rectangle.FromLTRB(Left,Top,Right,Bottom); }
    [StructLayout(LayoutKind.Sequential,Pack=1)] internal struct Blend { public byte Op,Flags,Alpha,Format; }
    [DllImport("user32.dll",SetLastError=true)] internal static extern bool UpdateLayeredWindow(nint h,nint dc,ref Point pos,ref Size size,nint src,ref Point origin,uint key,ref Blend blend,uint flags);
    [DllImport("user32.dll")] internal static extern nint GetDC(nint h);
    [DllImport("user32.dll")] internal static extern int ReleaseDC(nint h,nint dc);
    [DllImport("gdi32.dll")] internal static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32.dll")] internal static extern nint SelectObject(nint dc,nint obj);
    [DllImport("gdi32.dll")] internal static extern bool DeleteObject(nint obj);
    [DllImport("gdi32.dll")] internal static extern bool DeleteDC(nint dc);
    [StructLayout(LayoutKind.Sequential)] internal struct BitmapInfo {public uint Size;public int Width,Height;public ushort Planes,BitCount;public uint Compression,SizeImage;public int XPels,YPels;public uint Colors,Important;}
    [DllImport("gdi32.dll",SetLastError=true)] internal static extern nint CreateDIBSection(nint dc,ref BitmapInfo info,uint usage,out nint bits,nint section,uint offset);
    [DllImport("user32.dll",SetLastError=true)] internal static extern bool SetWindowDisplayAffinity(nint h,uint affinity);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(nint h,nint after,int x,int y,int w,int height,uint flags);
    [DllImport("user32.dll")] internal static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern nint WindowFromPoint(Point point);
    internal delegate nint HookProc(int code,nint message,nint data);
    [StructLayout(LayoutKind.Sequential)] internal struct MouseHookData {public Point Point;public uint MouseData,Flags,Time;public nuint ExtraInfo;}
    [DllImport("user32.dll",SetLastError=true)] internal static extern nint SetWindowsHookEx(int kind,HookProc proc,nint module,uint thread);
    [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")] internal static extern nint CallNextHookEx(nint hook,int code,nint message,nint data);
    internal delegate void WinEventProc(nint hook,uint evt,nint window,int obj,int child,uint thread,uint time);
    [DllImport("user32.dll")] internal static extern nint SetWinEventHook(uint min,uint max,nint module,WinEventProc callback,uint process,uint thread,uint flags);
    [DllImport("user32.dll")] internal static extern bool UnhookWinEvent(nint hook);
    [DllImport("user32.dll")] internal static extern bool GetWindowRect(nint h,out Rect rect);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(nint h);
    [DllImport("user32.dll")] internal static extern bool IsIconic(nint h);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern int GetClassName(nint h,StringBuilder name,int count);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(nint h,out uint id);
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] internal static extern nint GetWindowLongPtr(nint h,int index);
    internal delegate bool EnumProc(nint h,nint l);
    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumProc proc,nint l);
    [DllImport("dwmapi.dll")] internal static extern int DwmGetWindowAttribute(nint h,int attr,out int value,int size);
    [DllImport("dwmapi.dll")] internal static extern int DwmFlush();
    [DllImport("user32.dll",SetLastError=true)] internal static extern bool RegisterHotKey(nint h,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(nint h,int id);
    [DllImport("wtsapi32.dll")] internal static extern bool WTSRegisterSessionNotification(nint h,uint flags);
    [DllImport("wtsapi32.dll")] internal static extern bool WTSUnRegisterSessionNotification(nint h);
    internal static string Class(nint h) { var s=new StringBuilder(256); GetClassName(h,s,256); return s.ToString(); }
    internal static bool Desktop(nint h) => Class(h) is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd";
    internal static bool VisibleWindow(nint h)
    {
        if(!IsWindowVisible(h)||IsIconic(h)||Desktop(h))return false;
        return DwmGetWindowAttribute(h,14,out int cloaked,4)!=0||cloaked==0;
    }
    internal static List<Rectangle> Obstacles(ISet<nint> overlays)
    {
        var list=new List<Rectangle>();
        EnumWindows((h,_)=>{ if(!overlays.Contains(h)&&VisibleWindow(h)&&GetWindowRect(h,out var r)&&r.Right>r.Left&&r.Bottom>r.Top) list.Add(r.Rectangle); return true; },0);
        return list;
    }
    internal static string? ActiveDisplay(IReadOnlyList<Display> displays)
    {
        var h=GetForegroundWindow(); GetWindowThreadProcessId(h,out uint pid);
        if(pid==Environment.ProcessId||!VisibleWindow(h)||(GetWindowLongPtr(h,-20).ToInt64()&0x08000080)!=0||!GetWindowRect(h,out var r))return null;
        var ranked=displays.Select(d=>(d.Id,Area:Rectangle.Intersect(d.Bounds,r.Rectangle))).Select(p=>(p.Id,Area:(long)Math.Max(0,p.Area.Width)*Math.Max(0,p.Area.Height))).OrderByDescending(p=>p.Area).ToArray();
        if(ranked.Length==0||ranked[0].Area==0||(ranked.Length>1&&ranked[0].Area==ranked[1].Area))return null;
        return ranked[0].Id;
    }
}
