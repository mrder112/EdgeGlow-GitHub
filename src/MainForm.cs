using System.ComponentModel;
namespace EdgeGlow;
internal sealed class MainForm:Form
{
    private readonly Settings settings=Settings.Load();
    private readonly Engine engine;
    private readonly NotifyIcon tray;
    private readonly ComboBox source=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill};
    private readonly CheckedListBox targets=new(){CheckOnClick=true,Dock=DockStyle.Fill,Height=95};
    private readonly CheckBox follow=new(){Text="Следовать за активным окном (задержка 500 мс)",AutoSize=true};
    private readonly CheckBox trackClicks=new(){Text="Переключать источник щелчком по монитору",AutoSize=true};
    private readonly CheckBox captureDuringMove=new(){Text="Продолжать захват при перемещении окон",AutoSize=true};
    private readonly CheckBox noSmoothing=new(){Text="Полностью отключить сглаживание и размытие",AutoSize=true};
    private readonly ComboBox mode=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill};
    private readonly ComboBox fps=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill};
    private readonly ComboBox captureFps=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill};
    private readonly ComboBox samples=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill};
    private readonly Label status=new(){Dock=DockStyle.Fill,AutoSize=false,Height=65,Padding=new Padding(8),ForeColor=Color.FromArgb(30,80,120)};
    private readonly Dictionary<string,NumericUpDown> numbers=[];
    private readonly Panel scheme=new(){Height=150,Dock=DockStyle.Fill,BackColor=Color.FromArgb(240,243,246)};
    private readonly TextBox hotkey=new(){ReadOnly=true,Dock=DockStyle.Fill};
    private readonly TextBox moveHotkey=new(){ReadOnly=true,Dock=DockStyle.Fill};
    private readonly ComboBox calibrationTarget=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill};
    private string? calibrationKey;
    private int key;private uint modifiers;
    private int moveKey;private uint moveModifiers;
    private bool exiting, loading;
    private string topology="";
    internal MainForm()
    {
        Text="EdgeGlow · свечение между экранами";Icon=AppBrand.CreateIcon();ClientSize=new Size(720,770);MinimumSize=new Size(620,680);
        AutoScaleMode=AutoScaleMode.Dpi;Font=new Font("Segoe UI",10);
        engine=new Engine(settings);key=settings.HotKey;modifiers=settings.HotModifiers;moveKey=settings.MoveHotKey;moveModifiers=settings.MoveHotModifiers;
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(16),ColumnCount=1,RowCount=4};
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Absolute,75));
        root.Controls.Add(new Label{Text="EdgeGlow",Font=new Font(Font.FontFamily,22,FontStyle.Bold),AutoSize=true,Margin=new Padding(0,0,0,12)},0,0);
        var tabs=new TabControl{Dock=DockStyle.Fill};root.Controls.Add(tabs,0,1);
        var main=Page(tabs,"Экраны");
        Add(main,"Расположение Windows",scheme);scheme.Paint+=PaintScheme;
        Add(main,"Источник",source);Add(main,"Получатели",targets);Add(main,"",follow);Add(main,"",trackClicks);
        follow.CheckedChanged+=(_,_)=>{if(follow.Checked)trackClicks.Checked=false;};trackClicks.CheckedChanged+=(_,_)=>{if(trackClicks.Checked)follow.Checked=false;};
        mode.Items.AddRange(["Рабочий стол: скрывать при перекрытии окном","Поверх всех окон"]);mode.SelectedIndex=settings.AboveWindows?1:0;
        Add(main,"Отображение",mode);
        captureDuringMove.Checked=settings.CaptureDuringMove;Add(main,"Перетаскивание окон",captureDuringMove);
        Add(main,"",new Label{Text="В режиме рабочего стола весь ореол получателя скрывается, если окно пересекает область свечения. Это безопасный упрощённый режим.",AutoSize=true,MaximumSize=new Size(460,0)});
        Add(main,"",new Label{Text="Источник не получает собственного свечения. Для основного сценария выберите нижний экран источником, верхний — получателем.",AutoSize=true,MaximumSize=new Size(460,0)});
        var look=Page(tabs,"Свечение");
        samples.Items.AddRange(["32","64","128","256"]);samples.SelectedItem=settings.Samples.ToString();Add(look,"Разрешение выборки",samples);
        Add(look,"",new Label{Text="Цветовых точек вдоль края. Меньше — ниже плотность обработки и нагрузка GPU. Плавное растяжение ореола сохраняется.",AutoSize=true,MaximumSize=new Size(450,0)});
        Number(look,"brightness","Яркость, %",0,300,settings.Brightness*100);
        Number(look,"opacity","Макс. непрозрачность, %",0,100,settings.Opacity*100);
        Number(look,"saturation","Насыщенность, %",0,300,settings.Saturation*100);
        Number(look,"depth","Глубина, % экрана",5,100,settings.Depth*100);
        Number(look,"blur","Размытие вдоль стыка, %",0,25,settings.Blur*100,1);
        Number(look,"smoothing","Сглаживание, мс",0,2000,settings.Smoothing);
        noSmoothing.Checked=settings.NoSmoothing;Add(look,"Без сглаживания",noSmoothing);
        noSmoothing.CheckedChanged+=(_,_)=>{numbers["blur"].Enabled=numbers["smoothing"].Enabled=!noSmoothing.Checked;};
        numbers["blur"].Enabled=numbers["smoothing"].Enabled=!noSmoothing.Checked;
        Number(look,"strip","Полоса источника, %",.5m,25,settings.Strip*100,1);
        captureFps.Items.AddRange(["1","2","5","10","15","30","60"]);captureFps.SelectedItem=settings.CaptureFps.ToString();Add(look,"Захватов в секунду",captureFps);
        fps.Items.AddRange(["15","30","60"]);fps.SelectedItem=settings.Fps.ToString();Add(look,"Плавность, кадров/с",fps);
        var eco=new Button{Text="Щадящий режим: 5 захватов/с",AutoSize=true};eco.Click+=(_,_)=>{captureFps.SelectedItem="5";fps.SelectedItem="30";samples.SelectedItem="64";numbers["smoothing"].Value=400;noSmoothing.Checked=false;Apply();};Add(look,"Меньше нагрузка",eco);
        Add(look,"",new Label{Text="Изменения вступают в силу после «Применить» или запуска. Чёрный источник гасит эффект. Смешивание: обычное alpha-over.",AutoSize=true,MaximumSize=new Size(450,0)});
        var alignment=Page(tabs,"Калибровка");
        Add(alignment,"Получатель",calibrationTarget);
        Number(alignment,"offset","Сдвиг вдоль стыка, px",-10000,10000,0);
        Number(alignment,"scale","Масштаб стыка, %",25,400,100);
        Add(alignment,"",new Label{Text="Калибровка сохраняется отдельно для пары выбранный источник → получатель. Сдвиг измеряется в физических пикселях источника; масштаб задаётся относительно середины общего стыка.",AutoSize=true,MaximumSize=new Size(450,0)});
        Number(alignment,"left","Обрезка источника слева, px",0,10000,settings.CropLeft);
        Number(alignment,"top","Обрезка сверху, px",0,10000,settings.CropTop);
        Number(alignment,"right","Обрезка справа, px",0,10000,settings.CropRight);
        Number(alignment,"bottom","Обрезка снизу, px",0,10000,settings.CropBottom);
        Add(alignment,"",new Label{Text="Обрезка убирает чёрные полосы: край оставшегося изображения растягивается вдоль соответствующей границы источника.",AutoSize=true,MaximumSize=new Size(450,0)});
        var control=Page(tabs,"Управление");Add(control,"Быстро отключить",hotkey);UpdateHotKeyText();
        hotkey.KeyDown+=(_,e)=>{e.SuppressKeyPress=true;if(e.KeyCode is Keys.ControlKey or Keys.Menu or Keys.ShiftKey)return;key=(int)e.KeyCode;modifiers=(uint)((e.Alt?1:0)|(e.Control?2:0)|(e.Shift?4:0));UpdateHotKeyText();};
        Add(control,"Захват при движении",moveHotkey);
        moveHotkey.KeyDown+=(_,e)=>{e.SuppressKeyPress=true;if(e.KeyCode is Keys.ControlKey or Keys.Menu or Keys.ShiftKey)return;moveKey=(int)e.KeyCode;moveModifiers=(uint)((e.Alt?1:0)|(e.Control?2:0)|(e.Shift?4:0));UpdateHotKeyText();};
        Add(control,"",new Label{Text="Вторая горячая клавиша переключает захват при перетаскивании окон. По умолчанию Ctrl+Shift+F9. Захват при движении включён; выключите его, если появляются задержки.",AutoSize=true,MaximumSize=new Size(450,0)});
        Add(control,"",new Label{Text="Нажмите сочетание в поле выше, затем «Применить». Горячая клавиша только останавливает эффект. Закрытие окна настроек сворачивает программу в трей. Для завершения выберите «Выход».",AutoSize=true,MaximumSize=new Size(450,0)});
        Add(control,"",new Label{Text="Захват выполняется локально. Кадры не записываются и не отправляются в сеть. Базовый режим — SDR; HDR-источник будет отклонён. Эффект не включается автоматически при запуске.",AutoSize=true,MaximumSize=new Size(450,0)});
        var buttons=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,Padding=new Padding(0,12,0,0)};
        foreach(var (title,action) in new (string,Action)[]{("Включить",()=>{if(Apply())engine.Start();}),("Остановить",engine.Stop),("Тест без захвата",()=>{if(Apply())engine.Start(true);}),("Применить",()=>Apply()),("Сбросить параметры",ResetParameters)}){
            var b=new Button{Text=title,AutoSize=true,Height=34,Padding=new Padding(5)};b.Click+=(_,_)=>action();buttons.Controls.Add(b);
        }
        root.Controls.Add(buttons,0,2);root.Controls.Add(status,0,3);Controls.Add(root);
        var menu=new ContextMenuStrip();menu.Items.Add("Включить / приостановить",null,(_,_)=>{if(engine.Running)engine.Stop();else if(Apply())engine.Start();});menu.Items.Add("Настройки",null,(_,_)=>Open());menu.Items.Add("Выход",null,(_,_)=>Exit());
        tray=new NotifyIcon{Icon=Icon,Text="EdgeGlow",ContextMenuStrip=menu,Visible=true};tray.DoubleClick+=(_,_)=>Open();
        follow.Checked=settings.Follow;
        trackClicks.Checked=settings.TrackClicks;
        engine.Changed+=UpdateStatus;
        Populate();source.SelectedIndexChanged+=(_,_)=>{if(!loading){SaveCalibration();PopulateCalibration();scheme.Invalidate();}};
        targets.ItemCheck+=(_,_)=>BeginInvoke((Action)(()=>scheme.Invalidate()));
        calibrationTarget.SelectedIndexChanged+=(_,_)=>{if(!loading){SaveCalibration();LoadCalibration();}};
        Shown+=(_,_)=>{RegisterShortcut();Native.WTSRegisterSessionNotification(Handle,0);UpdateStatus();};
        FormClosing+=(_,e)=>{if(!exiting&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}};
    }
    private static TableLayoutPanel Page(TabControl tabs,string text){var page=new TabPage(text){AutoScroll=true,Padding=new Padding(8)};var table=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2};table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,215));table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));page.Controls.Add(table);tabs.TabPages.Add(page);return table;}
    private static void Add(TableLayoutPanel p,string label,Control control){int r=p.RowCount++;p.RowStyles.Add(new RowStyle(SizeType.AutoSize));p.Controls.Add(new Label{Text=label,AutoSize=true,Margin=new Padding(3,9,8,8)},0,r);control.Margin=new Padding(3,7,3,7);p.Controls.Add(control,1,r);}
    private void Number(TableLayoutPanel p,string id,string label,decimal min,decimal max,float value,int places=0){var n=new NumericUpDown{Minimum=min,Maximum=max,DecimalPlaces=places,Increment=places>0?.5m:1,Value=Math.Clamp((decimal)value,min,max),Dock=DockStyle.Fill};numbers[id]=n;Add(p,label,n);}
    private float N(string id)=>(float)numbers[id].Value;
    private void Populate()
    {
        loading=true;
        if(string.IsNullOrEmpty(settings.Source)){
            settings.Source=engine.Displays.OrderByDescending(d=>d.Bounds.Top).FirstOrDefault()?.Id??"";
            var s=engine.Displays.FirstOrDefault(d=>d.Id==settings.Source);
            if(s!=null)settings.Targets=engine.Displays.Where(d=>d.Id!=s.Id&&Geometry.Join(s.Bounds,d.Bounds)!=null).Select(d=>d.Id).ToList();
        }
        source.Items.Clear();targets.Items.Clear();foreach(var d in engine.Displays){source.Items.Add(d);targets.Items.Add(d,settings.Targets.Contains(d.Id));if(d.Id==settings.Source)source.SelectedItem=d;}
        topology=string.Join(";",engine.Displays);loading=false;PopulateCalibration();scheme.Invalidate();
    }
    private void PopulateCalibration(){loading=true;calibrationTarget.Items.Clear();foreach(var d in engine.Displays.Where(d=>d.Id!=(source.SelectedItem as Display)?.Id))calibrationTarget.Items.Add(d);if(calibrationTarget.Items.Count>0)calibrationTarget.SelectedIndex=0;loading=false;LoadCalibration();}
    private void SaveCalibration(){if(calibrationKey!=null)settings.Calibrations[calibrationKey]=new(){Offset=N("offset"),Scale=N("scale")/100};}
    private void LoadCalibration(){calibrationKey=source.SelectedItem is Display s&&calibrationTarget.SelectedItem is Display t?s.Id+"|"+t.Id:null;var c=calibrationKey!=null&&settings.Calibrations.TryGetValue(calibrationKey,out var v)?v:new Calibration();numbers["offset"].Value=(decimal)c.Offset;numbers["scale"].Value=(decimal)(c.Scale*100);}
    private bool Apply()
    {
        bool running=engine.Running;engine.Stop();
        SaveCalibration();settings.Source=(source.SelectedItem as Display)?.Id??"";settings.Targets=targets.CheckedItems.Cast<Display>().Select(d=>d.Id).ToList();
        settings.Follow=follow.Checked;settings.TrackClicks=trackClicks.Checked;settings.Samples=int.Parse((string)samples.SelectedItem!);settings.CaptureFps=int.Parse((string)captureFps.SelectedItem!);settings.AboveWindows=mode.SelectedIndex==1;settings.Brightness=N("brightness")/100;settings.Opacity=N("opacity")/100;settings.Saturation=N("saturation")/100;settings.Depth=N("depth")/100;settings.Blur=N("blur")/100;settings.Smoothing=N("smoothing");settings.Strip=N("strip")/100;settings.Fps=int.Parse((string)fps.SelectedItem!);
        settings.CropLeft=(int)N("left");settings.CropTop=(int)N("top");settings.CropRight=(int)N("right");settings.CropBottom=(int)N("bottom");settings.HotKey=key;settings.HotModifiers=modifiers;
        settings.CaptureDuringMove=captureDuringMove.Checked;settings.NoSmoothing=noSmoothing.Checked;settings.MoveHotKey=moveKey;settings.MoveHotModifiers=moveModifiers;
        if(!RegisterShortcut())return false;
        try{settings.Save();}catch(Exception e)when(e is IOException or UnauthorizedAccessException){MessageBox.Show(this,"Не удалось сохранить настройки: "+e.Message,"EdgeGlow");return false;}
        if(running)engine.Start();return true;
    }
    private static string KeyText(int key,uint mods)=>((mods&2)!=0?"Ctrl + ":"")+((mods&1)!=0?"Alt + ":"")+((mods&4)!=0?"Shift + ":"")+((Keys)key);
    private void UpdateHotKeyText(){hotkey.Text=KeyText(key,modifiers);moveHotkey.Text=KeyText(moveKey,moveModifiers);}
    private void ToggleMoveCapture(){settings.CaptureDuringMove=!settings.CaptureDuringMove;captureDuringMove.Checked=settings.CaptureDuringMove;try{settings.Save();}catch(Exception e)when(e is IOException or UnauthorizedAccessException){MessageBox.Show(this,"Не удалось сохранить режим: "+e.Message,"EdgeGlow");}status.Text=settings.CaptureDuringMove?"Захват при перемещении окон включён":"Захват при перемещении окон приостанавливается";tray.ShowBalloonTip(1800,"EdgeGlow",status.Text,ToolTipIcon.Info);}
    private void ResetParameters(){
        engine.Stop();var d=new Settings();
        captureDuringMove.Checked=d.CaptureDuringMove;noSmoothing.Checked=d.NoSmoothing;moveKey=d.MoveHotKey;moveModifiers=d.MoveHotModifiers;
        numbers["brightness"].Value=(decimal)d.Brightness*100;numbers["opacity"].Value=(decimal)d.Opacity*100;numbers["saturation"].Value=(decimal)d.Saturation*100;numbers["depth"].Value=(decimal)d.Depth*100;numbers["blur"].Value=(decimal)d.Blur*100;numbers["smoothing"].Value=(decimal)d.Smoothing;numbers["strip"].Value=(decimal)d.Strip*100;
        foreach(var id in new[]{"left","top","right","bottom","offset"})numbers[id].Value=0;numbers["scale"].Value=100;settings.Calibrations.Clear();
        fps.SelectedItem="30";captureFps.SelectedItem="30";samples.SelectedItem="128";mode.SelectedIndex=0;follow.Checked=false;trackClicks.Checked=false;key=d.HotKey;modifiers=d.HotModifiers;UpdateHotKeyText();Apply();status.Text="Параметры сброшены. Выбор экранов сохранён; эффект остановлен.";
    }
    private bool RegisterShortcut(){
        if(key==moveKey&&modifiers==moveModifiers){MessageBox.Show(this,"Для остановки эффекта и захвата при движении нужны разные сочетания клавиш.","EdgeGlow");return false;}
        Native.UnregisterHotKey(Handle,1);Native.UnregisterHotKey(Handle,2);
        bool stop=Native.RegisterHotKey(Handle,1,modifiers|0x4000,(uint)key);int stopError=System.Runtime.InteropServices.Marshal.GetLastWin32Error();
        bool move=Native.RegisterHotKey(Handle,2,moveModifiers|0x4000,(uint)moveKey);int moveError=System.Runtime.InteropServices.Marshal.GetLastWin32Error();
        if(stop&&move)return true;MessageBox.Show(this,"Не удалось зарегистрировать сочетание: "+(!stop?"остановка эффекта ("+KeyText(key,modifiers)+")":"захват при движении ("+KeyText(moveKey,moveModifiers)+")")+". Выберите другое сочетание.\n"+new Win32Exception(!stop?stopError:moveError).Message,"EdgeGlow",MessageBoxButtons.OK,MessageBoxIcon.Warning);return false;
    }
    private void UpdateStatus(){if(IsDisposed)return;status.Text=engine.Status;if(topology!=string.Join(";",engine.Displays))Populate();}
    private void PaintScheme(object? sender,PaintEventArgs e)
    {
        if(engine.Displays.Count==0)return;var all=engine.Displays.Select(d=>d.Bounds).Aggregate(Rectangle.Union);float scale=Math.Min((scheme.Width-16f)/all.Width,(scheme.Height-16f)/all.Height);
        foreach(var d in engine.Displays){var r=new RectangleF(8+(d.Bounds.X-all.X)*scale,8+(d.Bounds.Y-all.Y)*scale,d.Bounds.Width*scale-3,d.Bounds.Height*scale-3);bool src=d.Id==(source.SelectedItem as Display)?.Id;using var b=new SolidBrush(src?Color.FromArgb(34,112,172):Color.FromArgb(185,206,219));e.Graphics.FillRectangle(b,r);e.Graphics.DrawString(d.Id.Replace(@"\\.\","")+(src?"\nИсточник":""),Font,src?Brushes.White:Brushes.Black,r);}
    }
    private void Open(){Show();WindowState=FormWindowState.Normal;Activate();}
    private void Exit(){exiting=true;Close();}
    protected override void WndProc(ref Message m)
    {
        if(m.Msg==0x312){if(m.WParam==1)engine?.Stop();else if(m.WParam==2)ToggleMoveCapture();return;}
        if(m.Msg==0x7E)BeginInvoke((Action)(()=>engine.ConfigurationChanged()));
        if(m.Msg==0x2B1){if(m.WParam==7)engine.Suspend(true);else if(m.WParam==8)engine.Suspend(false);}
        if(m.Msg==0x218){if(m.WParam==4)engine.Suspend(true);else if(m.WParam==7||m.WParam==18)engine.Suspend(false);}
        base.WndProc(ref m);
    }
    protected override void Dispose(bool disposing){if(disposing){Native.UnregisterHotKey(Handle,1);Native.UnregisterHotKey(Handle,2);Native.WTSUnRegisterSessionNotification(Handle);engine.Dispose();tray.Visible=false;tray.Dispose();}base.Dispose(disposing);}
}
