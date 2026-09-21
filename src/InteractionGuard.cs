namespace EdgeGlow;
internal sealed class InteractionGuard:IDisposable
{
    private readonly Native.WinEventProc callback;
    private readonly nint hook;
    private bool moving;
    private long resumeAt;
    internal bool Paused=>moving||Environment.TickCount64<resumeAt;
    internal InteractionGuard(Action hide)
    {
        callback=(_,evt,_,_,_,_,_)=>{if(evt==10){moving=true;hide();}else if(evt==11){moving=false;resumeAt=Environment.TickCount64+200;}};
        hook=Native.SetWinEventHook(10,11,0,callback,0,0,0);
        if(hook==0)throw new System.ComponentModel.Win32Exception();
    }
    public void Dispose(){Native.UnhookWinEvent(hook);GC.KeepAlive(callback);}
}
