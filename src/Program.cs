using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PBZAchievements {
static class Texts {
 public static int Language=0;
 public static string T(string en,string zh,string tw){return Language==1?zh:Language==2?tw:en;}
}
static class Native {
 const string D="steam_api64.dll";
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] public static extern IntPtr LoadLibraryEx(string path,IntPtr file,uint flags);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] public static extern bool SteamAPI_InitSafe();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void SteamAPI_Shutdown();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern IntPtr SteamAPI_SteamUserStats_v013();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern IntPtr SteamAPI_SteamUser_v023();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern ulong SteamAPI_ISteamUser_GetSteamID(IntPtr p);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] public static extern bool SteamAPI_ISteamUserStats_GetAchievement(IntPtr p,string id,[MarshalAs(UnmanagedType.I1)] out bool value);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern IntPtr SteamAPI_ISteamUserStats_GetAchievementDisplayAttribute(IntPtr p,string id,string key);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] public static extern bool SteamAPI_ISteamUserStats_SetAchievement(IntPtr p,string id);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] public static extern bool SteamAPI_ISteamUserStats_StoreStats(IntPtr p);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void SteamAPI_ManualDispatch_Init();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern int SteamAPI_GetHSteamPipe();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void SteamAPI_ManualDispatch_RunFrame(int pipe);
 [StructLayout(LayoutKind.Sequential)] public struct Message {public int user,id;public IntPtr data;public int size;}
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] public static extern bool SteamAPI_ManualDispatch_GetNextCallback(int pipe,out Message message);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void SteamAPI_ManualDispatch_FreeLastCallback(int pipe);
 public static string Utf8(IntPtr p){if(p==IntPtr.Zero)return "";int n=0;while(Marshal.ReadByte(p,n)!=0&&n<65536)n++;byte[] b=new byte[n];Marshal.Copy(p,b,0,n);return System.Text.Encoding.UTF8.GetString(b);}
}
sealed class Session : IDisposable {
 public IntPtr Stats; public ulong User; public bool Connected; int pipe; int result;
 public async Task Connect(string dll){
  if(Native.LoadLibraryEx(Path.GetFullPath(dll),IntPtr.Zero,0x1100)==IntPtr.Zero)throw new Exception(Texts.T("Could not load the game's x64 Steam library.","无法加载游戏的 64 位 Steam 库。","無法載入遊戲的 64 位 Steam 程式庫。"));
  Environment.SetEnvironmentVariable("SteamAppId","4115450");Environment.SetEnvironmentVariable("SteamGameId","4115450");
  if(!Native.SteamAPI_InitSafe())throw new Exception(Texts.T("Start Steam, sign in, and ensure this account has access to the game.","请启动 Steam、登录，并确认此账号有权访问游戏。","請啟動 Steam、登入，並確認此帳號有權存取遊戲。"));
  Connected=true;Native.SteamAPI_ManualDispatch_Init();pipe=Native.SteamAPI_GetHSteamPipe();Stats=Native.SteamAPI_SteamUserStats_v013();User=Native.SteamAPI_ISteamUser_GetSteamID(Native.SteamAPI_SteamUser_v023());
  for(int i=0;i<150;i++){Pump();bool value;if(Native.SteamAPI_ISteamUserStats_GetAchievement(Stats,"ACH_001",out value))return;await Task.Delay(100);}
  throw new Exception(Texts.T("Steam stats timed out. Try connecting again.","读取 Steam 成就超时，请重新连接。","讀取 Steam 成就逾時，請重新連線。"));
 }
 void Pump(){Native.SteamAPI_ManualDispatch_RunFrame(pipe);Native.Message m;while(Native.SteamAPI_ManualDispatch_GetNextCallback(pipe,out m)){try{if(m.id==1102&&m.size>=12&&(ulong)Marshal.ReadInt64(m.data)==4115450UL)result=Marshal.ReadInt32(m.data,8);}finally{Native.SteamAPI_ManualDispatch_FreeLastCallback(pipe);}}}
 public bool Read(string id){bool value;if(!Native.SteamAPI_ISteamUserStats_GetAchievement(Stats,id,out value))throw new Exception(Texts.T("Cannot read achievement: ","无法读取成就：","無法讀取成就：")+id);return value;}
 public async Task Unlock(List<string> ids){
  Pump();result=0;foreach(string id in ids)if(!Read(id)&&!Native.SteamAPI_ISteamUserStats_SetAchievement(Stats,id))throw new Exception("SetAchievement: "+id);
  if(!Native.SteamAPI_ISteamUserStats_StoreStats(Stats))throw new Exception("StoreStats: false");
  for(int i=0;i<200&&result==0;i++){Pump();await Task.Delay(100);}
  if(result!=1)throw new Exception(Texts.T("Steam did not confirm saving. Result: ","Steam 未确认保存成功，结果：","Steam 未確認儲存成功，結果：")+result);
  foreach(string id in ids)if(!Read(id))throw new Exception("Verification: "+id);
 }
 public void Dispose(){if(Connected){Native.SteamAPI_Shutdown();Connected=false;}}
}
sealed class Window : Form {
 ComboBox language=new ComboBox(); TextBox path=new TextBox(); Button browse=new Button(),connect=new Button(),refresh=new Button(),selected=new Button(),all=new Button();
 Label heading=new Label(),subtitle=new Label(),account=new Label(),status=new Label();ListView list=new ListView();Session session;bool busy;
 public Window(){
  Text="PBZ Achievement Tool";Size=new Size(1000,760);MinimumSize=new Size(800,600);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(244,246,250);
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(24),ColumnCount=1,RowCount=7};Controls.Add(layout);
  layout.RowStyles.Add(new RowStyle(SizeType.Absolute,50));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,42));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,38));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,58));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,45));
  var top=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,160));heading.Font=new Font("Segoe UI",21,FontStyle.Bold);heading.Dock=DockStyle.Fill;top.Controls.Add(heading);language.Items.AddRange(new object[]{"English","简体中文","繁體中文"});language.DropDownStyle=ComboBoxStyle.DropDownList;language.Dock=DockStyle.Fill;top.Controls.Add(language);layout.Controls.Add(top);
  subtitle.Dock=DockStyle.Fill;layout.Controls.Add(subtitle);
  var files=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3};files.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));files.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,110));files.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,120));path.Dock=DockStyle.Fill;files.Controls.Add(path);files.Controls.Add(browse);files.Controls.Add(connect);layout.Controls.Add(files);
  account.Dock=DockStyle.Fill;layout.Controls.Add(account);list.Dock=DockStyle.Fill;list.View=View.Details;list.CheckBoxes=true;list.FullRowSelect=true;list.GridLines=false;list.Columns.Add("ID",110);list.Columns.Add("Achievement",540);list.Columns.Add("Status",150);layout.Controls.Add(list);
  var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(0,8,0,0)};actions.Controls.Add(all);actions.Controls.Add(selected);actions.Controls.Add(refresh);layout.Controls.Add(actions);status.Dock=DockStyle.Fill;layout.Controls.Add(status);
  foreach(var b in new[]{browse,connect,refresh,selected,all}){b.AutoSize=true;b.Height=34;b.Padding=new Padding(9,3,9,3);b.FlatStyle=FlatStyle.Flat;}
  all.BackColor=Color.FromArgb(40,74,145);all.ForeColor=Color.White;
  language.SelectedIndexChanged+=(s,e)=>{Texts.Language=language.SelectedIndex;Translate();if(session!=null&&session.Connected&&!busy)Render();};language.SelectedIndex=System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("zh")?1:0;
  browse.Click+=(s,e)=>{using(var d=new OpenFileDialog{Filter="Steam library (steam_api64.dll)|steam_api64.dll",Title="PBZ / steam_api64.dll"})if(d.ShowDialog()==DialogResult.OK)path.Text=d.FileName;};
  connect.Click+=async(s,e)=>await Run(async()=>{if(session!=null)session.Dispose();session=new Session();await session.Connect(path.Text);Render();});
  refresh.Click+=async(s,e)=>await Run(()=>{Render();return Task.FromResult(0);});selected.Click+=async(s,e)=>await Submit(false);all.Click+=async(s,e)=>await Submit(true);
  FormClosing+=(s,e)=>{if(busy){e.Cancel=true;return;}if(session!=null)session.Dispose();};Translate();Enable();
 }
 void Translate(){heading.Text="PBZ Achievement Tool";subtitle.Text=Texts.T("Phantom Blade Zero · Steam achievements · No save editing","影之刃零 · Steam 成就管理 · 无需修改存档","影之刃零 · Steam 成就管理 · 無需修改存檔");browse.Text=Texts.T("Browse…","选择 DLL…","選擇 DLL…");connect.Text=Texts.T("Connect","连接 Steam","連線 Steam");refresh.Text=Texts.T("Refresh","刷新","重新整理");selected.Text=Texts.T("Unlock selected","解锁勾选项","解鎖勾選項");all.Text=Texts.T("Unlock all","一键全部解锁","一鍵全部解鎖");list.Columns[1].Text=Texts.T("Achievement","成就","成就");list.Columns[2].Text=Texts.T("Status","状态","狀態");if(session==null||!session.Connected)account.Text=Texts.T("Select steam_api64.dll from your PBZ installation to connect.","选择影之刃零安装目录中的 steam_api64.dll 后连接。","選擇影之刃零安裝目錄中的 steam_api64.dll 後連線。");status.Text=Texts.T("Ready. Changes are made only when you click an unlock button.","就绪。点击解锁按钮后才会修改成就。","就緒。點擊解鎖按鈕後才會修改成就。");}
 void Enable(){bool ok=session!=null&&session.Connected;connect.Enabled=browse.Enabled=path.Enabled=language.Enabled=!busy;refresh.Enabled=selected.Enabled=all.Enabled=ok&&!busy;}
 async Task Run(Func<Task> action){if(busy)return;busy=true;Enable();status.Text=Texts.T("Working…","处理中…","處理中…");try{await action();status.Text=Texts.T("Complete.","完成。","完成。");}catch(Exception e){status.Text=e.Message;MessageBox.Show(this,e.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Error);if(session!=null){session.Dispose();session=null;}}finally{busy=false;Enable();}}
 void Render(){list.BeginUpdate();try{list.Items.Clear();int count=0;for(int i=1;i<=66;i++){string id="ACH_"+i.ToString("000");bool done=session.Read(id);if(done)count++;string name=Native.Utf8(Native.SteamAPI_ISteamUserStats_GetAchievementDisplayAttribute(session.Stats,id,"name"));var row=new ListViewItem(id);row.SubItems.Add(name.Length==0?id:name);row.SubItems.Add(done?Texts.T("Unlocked","已解锁","已解鎖"):Texts.T("Locked","未解锁","未解鎖"));row.ForeColor=done?Color.FromArgb(31,115,78):Color.FromArgb(55,65,80);list.Items.Add(row);}account.Text="Steam ID: "+session.User+"    •    "+count+" / 66";}finally{list.EndUpdate();}}
 async Task Submit(bool every){await Run(async()=>{var ids=new List<string>();foreach(ListViewItem row in list.Items)if((every||row.Checked)&&!session.Read(row.Text))ids.Add(row.Text);if(ids.Count==0){Render();return;}await session.Unlock(ids);Render();status.Text=Texts.T("Steam confirmed saving.","Steam 已确认保存。","Steam 已確認儲存。");});}
}
static class Program {
 [STAThread] static void Main(string[] args){if(args.Length==3&&args[0]=="--check"){try{using(var session=new Session()){session.Connect(args[1]).GetAwaiter().GetResult();int count=0;for(int i=1;i<=66;i++)if(session.Read("ACH_"+i.ToString("000")))count++;File.WriteAllText(args[2],"Read-only Steam check passed. Unlocked="+count+"/66");}}catch(Exception e){File.WriteAllText(args[2],e.ToString());Environment.ExitCode=1;}return;}Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);if(args.Length==2&&args[0]=="--preview"){using(var w=new Window()){w.Show();Application.DoEvents();using(var b=new Bitmap(w.Width,w.Height)){w.DrawToBitmap(b,new Rectangle(0,0,b.Width,b.Height));b.Save(args[1]);}}return;}Application.Run(new Window());}
}
}

