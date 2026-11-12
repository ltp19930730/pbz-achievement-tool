using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PBZAchievements {
static partial class Texts {
 public static int Language=0;
 public static string T(string en,string zh,string tw){return Language==1?zh:Language==2?tw:Localize(en);}
}
static partial class Native {
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
sealed partial class Session : IDisposable {
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

static class Program {
 [STAThread] static void Main(string[] args) {
  if(args.Length==2&&args[0]=="--discover"){File.WriteAllText(args[1],Discovery.FindLibrary()??"NOT_FOUND");return;}if(args.Length==3&&args[0]=="--check") {
   try {using(var session=new Session()) {session.Connect(args[1]).GetAwaiter().GetResult();int count=0,icons=0;for(int i=1;i<=66;i++){string id="ACH_"+i.ToString("000");if(session.Read(id))count++;using(var icon=session.Icon(id))if(icon!=null)icons++;}File.WriteAllText(args[2],"Read-only Steam check passed. Unlocked="+count+"/66; icons="+icons);}}
   catch(Exception e){File.WriteAllText(args[2],e.ToString());Environment.ExitCode=1;}return;
  }
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  if(args.Length==3&&args[0]=="--preview-guide"){Texts.Language=int.Parse(args[2]);Guides.Show(null,new Achievement{Id="ACH_038",Name="The Healer’s Legacy",Description="Complete [Flesh Lily], [Apprentice Healer], and [A Healer’s Mercy]"},args[1]);return;}
  var window=new Window(args.Length==0);if(args.Length==2&&args[0]=="--ui-check"){try{window.Show();window.VerifyUi();File.WriteAllText(args[1],"PASS: 15 languages, menu selection, loading animation frames, locked filter, ID search, empty state, selection persistence, narrow layout, preview write prevention.");}catch(Exception e){File.WriteAllText(args[1],e.ToString());Environment.ExitCode=1;}finally{window.Close();}return;}
  if(args.Length==3&&args[0]=="--live-ui-check"){window.Shown+=async(s,e)=>{try{await window.ReadOnlyPreview(args[2]);window.VerifyLiveMenus();File.WriteAllText(args[1],"PASS: connected Steam with real icons; 30 language and 30 filter menu selections; read-only.");}catch(Exception error){File.WriteAllText(args[1],error.ToString());Environment.ExitCode=1;}finally{window.Close();}};Application.Run(window);return;}if(args.Length>=2&&(args[0]=="--preview"||args[0]=="--preview-live")) {
   window.Shown+=async(s,e)=>{try{if(args[0]=="--preview-live")await window.ReadOnlyPreview(args[2],args.Length>=4?int.Parse(args[3]):0);else window.Preview(args.Length>=3?int.Parse(args[2]):0);await Task.Delay(300);using(var bitmap=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(bitmap,new Rectangle(0,0,bitmap.Width,bitmap.Height));bitmap.Save(args[1]);}}catch(Exception error){File.WriteAllText(args[1]+".error.txt",error.ToString());Environment.ExitCode=1;}finally{window.Close();}};
  }
  Application.Run(window);
 }
}
}




