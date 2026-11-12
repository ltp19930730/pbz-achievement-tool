using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PBZAchievements {
static partial class Native {
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern int SteamAPI_ISteamUserStats_GetAchievementIcon(IntPtr p,string id);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern IntPtr SteamAPI_SteamUtils_v011();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] public static extern bool SteamAPI_ISteamUtils_GetImageSize(IntPtr p,int image,out uint width,out uint height);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] public static extern bool SteamAPI_ISteamUtils_GetImageRGBA(IntPtr p,int image,byte[] data,int size);
}
sealed partial class Session {
 public Bitmap Icon(string id) {
  int handle=Native.SteamAPI_ISteamUserStats_GetAchievementIcon(Stats,id);
  if(handle<=0)return null;
  uint w,h;IntPtr utils=Native.SteamAPI_SteamUtils_v011();
  if(!Native.SteamAPI_ISteamUtils_GetImageSize(utils,handle,out w,out h)||w==0||h==0||w>1024||h>1024)return null;
  byte[] pixels=new byte[checked((int)(w*h*4))];
  if(!Native.SteamAPI_ISteamUtils_GetImageRGBA(utils,handle,pixels,pixels.Length))return null;
  for(int i=0;i<pixels.Length;i+=4){byte r=pixels[i];pixels[i]=pixels[i+2];pixels[i+2]=r;}
  var bitmap=new Bitmap((int)w,(int)h,PixelFormat.Format32bppArgb);
  var data=bitmap.LockBits(new Rectangle(0,0,(int)w,(int)h),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
  try{for(int y=0;y<h;y++)Marshal.Copy(pixels,y*(int)w*4,IntPtr.Add(data.Scan0,y*data.Stride),(int)w*4);}finally{bitmap.UnlockBits(data);}
  return bitmap;
 }
 public void Tick(){Pump();}
 public string Attribute(string id,string attribute){return Native.Utf8(Native.SteamAPI_ISteamUserStats_GetAchievementDisplayAttribute(Stats,id,attribute));}
}
sealed class Achievement : IDisposable {
 public string Id,Name,Description;public bool Done;public Bitmap Image;
 public void Dispose(){if(Image!=null)Image.Dispose();}
}
class DarkButton : Button {
 protected override void OnPaint(PaintEventArgs e) {
  Color fill=Enabled?BackColor:Theme.Surface;
  using(var brush=new SolidBrush(fill))e.Graphics.FillRectangle(brush,ClientRectangle);
  using(var pen=new Pen(Focused?Theme.Gold:Theme.Border))e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1);
  TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Enabled?ForeColor:Theme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
 }
}
sealed class DarkChoice : DarkButton {
 public readonly List<object> Items=new List<object>();
 int index=-1;
 public event EventHandler SelectedIndexChanged;
 public int SelectedIndex {get{return index;}set{index=value;Text=index>=0&&index<Items.Count?Items[index].ToString()+"   ▾":"";Invalidate();if(SelectedIndexChanged!=null)SelectedIndexChanged(this,EventArgs.Empty);}}
 public DarkChoice(){Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.ComboBox;}
 protected override void OnClick(EventArgs e){base.OnClick(e);var menu=new ContextMenuStrip{BackColor=Theme.Surface,ForeColor=Theme.Text,ShowImageMargin=false};for(int i=0;i<Items.Count;i++){int choice=i;var item=new ToolStripMenuItem(Items[i].ToString()){Checked=choice==index};item.Click+=(s,a)=>SelectedIndex=choice;menu.Items.Add(item);}menu.Closed+=(s,a)=>menu.Dispose();menu.Show(this,new Point(0,Height));}
}
static class Theme {
 public static readonly Color Background=Color.FromArgb(17,21,27),Surface=Color.FromArgb(27,33,41),Border=Color.FromArgb(48,57,68),Text=Color.FromArgb(237,236,230),Muted=Color.FromArgb(153,166,179),Gold=Color.FromArgb(214,183,120),Green=Color.FromArgb(127,193,160);
 public static Button Button(string text,bool primary=false){return new DarkButton{Text=text,AutoSize=false,Height=36,Width=130,FlatStyle=FlatStyle.Flat,BackColor=primary?Gold:Surface,ForeColor=primary?Background:Text,Cursor=Cursors.Hand,Margin=new Padding(4)};}
 public static Label Label(string text,int size,Color color){return new Label{Text=text,AutoSize=false,Font=new Font("Segoe UI",size),ForeColor=color,BackColor=Color.Transparent};}
}
sealed class AchievementCard : Panel {
 public readonly Achievement Item; public readonly CheckBox Selection=new CheckBox();public readonly Button Unlock;
 public AchievementCard(Achievement item,Action<string> unlock,Action selectionChanged) {
  Item=item;DoubleBuffered=true;Height=158;BackColor=Theme.Surface;Padding=new Padding(16);Margin=new Padding(0,0,12,12);
  var image=new PictureBox{Location=new Point(16,18),Size=new Size(64,64),SizeMode=PictureBoxSizeMode.Zoom,Image=item.Image,BackColor=Theme.Background};Controls.Add(image);var guide=new LinkLabel{Text=Texts.T("Guide ↗","攻略 ↗","攻略 ↗"),Location=new Point(16,86),Size=new Size(70,24),LinkColor=Theme.Gold,ActiveLinkColor=Theme.Text,VisitedLinkColor=Theme.Gold};guide.LinkClicked+=(s,e)=>Guides.Show(FindForm(),item);Controls.Add(guide);
  if(item.Image==null){var fallback=Theme.Label(item.Id.Substring(4),19,Theme.Gold);fallback.Dock=DockStyle.Fill;fallback.TextAlign=ContentAlignment.MiddleCenter;image.Controls.Add(fallback);}
  var title=Theme.Label(item.Name,11,Theme.Text);title.Font=new Font("Segoe UI",11,FontStyle.Bold);title.Location=new Point(94,17);title.Height=30;title.AutoEllipsis=true;title.Cursor=Cursors.Hand;title.Click+=(s,e)=>Guides.Show(FindForm(),item);Controls.Add(title);
  var description=Theme.Label(string.IsNullOrWhiteSpace(item.Description)?Texts.T("Hidden achievement","隐藏成就","隱藏成就"):item.Description,9,Theme.Muted);description.Location=new Point(94,49);description.Height=54;description.AutoEllipsis=true;Controls.Add(description);
  var state=Theme.Label(item.Done?Texts.T("UNLOCKED","已解锁","已解鎖"):Texts.T("LOCKED","未解锁","未解鎖"),9,item.Done?Theme.Green:Theme.Gold);state.Location=new Point(16,116);state.Size=new Size(115,25);Controls.Add(state);
  Selection.Text=Texts.T("Select","勾选","勾選");Selection.ForeColor=Theme.Muted;Selection.AutoSize=true;Selection.Visible=!item.Done;Selection.CheckedChanged+=(s,e)=>selectionChanged();Controls.Add(Selection);
  Unlock=Theme.Button(item.Done?Texts.T("Completed","已完成","已完成"):Texts.T("Unlock","解锁","解鎖"),!item.Done);Unlock.Enabled=!item.Done;Unlock.Width=112;Unlock.Click+=(s,e)=>unlock(item.Id);Controls.Add(Unlock);
  Resize+=(s,e)=>{title.Width=description.Width=Math.Max(100,Width-112);Unlock.Location=new Point(Width-128,108);Selection.Location=new Point(Width-217,116);};
  Paint+=(s,e)=>{using(var pen=new Pen(Theme.Border))e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1);};
 }
}
sealed class Window : Form {
 readonly DarkChoice language=new DarkChoice(),filter=new DarkChoice();readonly TextBox path=new TextBox(),search=new TextBox();
 readonly Button browse=Theme.Button(""),connect=Theme.Button("",true),refresh=Theme.Button(""),selected=Theme.Button(""),all=Theme.Button("",true);
 readonly Label title=Theme.Label("",25,Theme.Text),subtitle=Theme.Label("",10,Theme.Muted),account=Theme.Label("",10,Theme.Muted),summary=Theme.Label("",13,Theme.Gold),status=Theme.Label("",9,Theme.Muted),searchLabel=Theme.Label("",10,Theme.Muted),empty=Theme.Label("",13,Theme.Muted);
 readonly FlowLayoutPanel cards=new FlowLayoutPanel();readonly Panel progress=new Panel();readonly ToolTip tips=new ToolTip();readonly Timer timer=new Timer();
 readonly List<Achievement> items=new List<Achievement>();readonly HashSet<string> chosen=new HashSet<string>();Session session;bool busy,rendering;bool preview;
 public Window(bool autoConnect=false) {
  Text="PBZ Achievement Tool · 1.2";Size=new Size(1120,860);MinimumSize=new Size(820,700);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);BackColor=Theme.Background;ForeColor=Theme.Text;AutoScaleMode=AutoScaleMode.Dpi;
  var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(28,22,28,16),ColumnCount=1,RowCount=9};Controls.Add(root);
  foreach(int height in new[]{54,32,54,36,14,52})root.RowStyles.Add(new RowStyle(SizeType.Absolute,height));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,56));root.RowStyles.Add(new RowStyle(SizeType.Absolute,30));
  var header=Row(170);title.Dock=DockStyle.Fill;header.Controls.Add(title,0,0);language.Items.AddRange(new object[]{"English","简体中文","繁體中文"});Style(language);header.Controls.Add(language,1,0);root.Controls.Add(header,0,0);subtitle.Dock=DockStyle.Fill;root.Controls.Add(subtitle,0,1);
  var connection=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,Padding=new Padding(0,7,0,0)};connection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));connection.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,120));connection.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,150));Style(path);connection.Controls.Add(path,0,0);browse.Dock=connect.Dock=DockStyle.Fill;connection.Controls.Add(browse,1,0);connection.Controls.Add(connect,2,0);root.Controls.Add(connection,0,2);
  var info=Row(290);account.Dock=summary.Dock=DockStyle.Fill;summary.TextAlign=ContentAlignment.MiddleRight;info.Controls.Add(account,0,0);info.Controls.Add(summary,1,0);root.Controls.Add(info,0,3);progress.Dock=DockStyle.Fill;progress.Margin=new Padding(0,3,0,7);progress.BackColor=Theme.Surface;progress.Paint+=(s,e)=>{int count=items.Count(x=>x.Done);if(items.Count>0)using(var brush=new SolidBrush(Theme.Gold))e.Graphics.FillRectangle(brush,0,0,progress.Width*count/items.Count,progress.Height);};root.Controls.Add(progress,0,4);
  var find=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,Padding=new Padding(0,8,0,4)};find.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,82));find.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));find.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,190));searchLabel.Dock=DockStyle.Fill;searchLabel.TextAlign=ContentAlignment.MiddleLeft;Style(search);Style(filter);find.Controls.Add(searchLabel,0,0);find.Controls.Add(search,1,0);find.Controls.Add(filter,2,0);root.Controls.Add(find,0,5);
  cards.Dock=DockStyle.Fill;cards.AutoScroll=true;cards.Margin=new Padding(0);cards.Resize+=(s,e)=>SizeCards();empty.Size=new Size(600,130);empty.TextAlign=ContentAlignment.MiddleCenter;root.Controls.Add(cards,0,6);
  var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(0,10,0,0)};all.Width=180;selected.Width=200;refresh.Width=120;actions.Controls.Add(all);actions.Controls.Add(selected);actions.Controls.Add(refresh);root.Controls.Add(actions,0,7);status.Dock=DockStyle.Fill;status.AutoEllipsis=true;root.Controls.Add(status,0,8);
  language.SelectedIndexChanged+=(s,e)=>{Texts.Language=language.SelectedIndex;Translate();DrawCards();};filter.SelectedIndexChanged+=(s,e)=>{if(!rendering)DrawCards();};search.TextChanged+=(s,e)=>DrawCards();
  language.SelectedIndex=System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("zh")?1:0;
  browse.Click+=(s,e)=>{using(var dialog=new OpenFileDialog{Filter="Steam library|steam_api64.dll"})if(dialog.ShowDialog(this)==DialogResult.OK)path.Text=dialog.FileName;};
  connect.Click+=async(s,e)=>await Run(async()=>{ClearItems();if(session!=null)session.Dispose();session=new Session();await session.Connect(path.Text);Reload();},Texts.T("Connected to Steam.","已连接 Steam。","已連線 Steam。"));
  refresh.Click+=async(s,e)=>await Run(()=>{Reload();return Task.FromResult(0);},Texts.T("Achievements refreshed.","成就状态已刷新。","成就狀態已重新整理。"));
  selected.Click+=async(s,e)=>await Submit(chosen.ToList());all.Click+=async(s,e)=>await Submit(items.Where(x=>!x.Done).Select(x=>x.Id).ToList());
  timer.Interval=800;timer.Tick+=(s,e)=>{if(busy||session==null||!session.Connected)return;try{session.Tick();bool changed=false;foreach(var item in items.Where(x=>x.Image==null)){item.Image=session.Icon(item.Id);changed|=item.Image!=null;}if(changed)DrawCards();}catch{timer.Stop();}};timer.Start();
  FormClosing+=(s,e)=>{if(busy)e.Cancel=true;};FormClosed+=(s,e)=>{timer.Dispose();ClearItems();if(session!=null)session.Dispose();tips.Dispose();};Translate();EnableControls();if(autoConnect)Shown+=async(s,e)=>await AutoConnect();
 }
 public async Task AutoConnect(){await Run(async()=>{string dll=Discovery.FindLibrary();if(dll==null)throw new Exception(Texts.T("Game installation not found. Select the game Steam library manually.","未找到游戏安装位置，请手动选择游戏的 Steam 库文件。","未找到遊戲安裝位置，請手動選擇遊戲的 Steam 程式庫。"));path.Text=dll;session=new Session();await session.Connect(dll);Reload();},Texts.T("Automatically connected to Steam.","已自动连接 Steam。","已自動連線 Steam。"));} static TableLayoutPanel Row(int right){var row=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Margin=new Padding(0)};row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,right));return row;}
 static void Style(Control c){c.Dock=DockStyle.Fill;c.Margin=new Padding(4);c.BackColor=Theme.Surface;c.ForeColor=Theme.Text;var text=c as TextBox;if(text!=null)text.BorderStyle=BorderStyle.FixedSingle;}
 void Translate(){
  title.Text=Texts.T("PHANTOM BLADE ZERO","影之刃零","影之刃零");subtitle.Text=Texts.T("ACHIEVEMENT COLLECTION  /  Steam community tool","成就图鉴  /  Steam 社区工具","成就圖鑑  /  Steam 社群工具");browse.Text=Texts.T("Browse…","选择文件…","選擇檔案…");connect.Text=Texts.T("Connect Steam","连接 Steam","連線 Steam");refresh.Text=Texts.T("Refresh","刷新状态","重新整理");all.Text=Texts.T("Unlock all remaining","解锁全部未完成","解鎖全部未完成");searchLabel.Text=Texts.T("Search","搜索成就","搜尋成就");
  rendering=true;int index=filter.SelectedIndex;filter.Items.Clear();filter.Items.AddRange(new object[]{Texts.T("All achievements","全部成就","全部成就"),Texts.T("Locked only","仅未解锁","僅未解鎖"),Texts.T("Unlocked only","仅已解锁","僅已解鎖")});filter.SelectedIndex=Math.Max(0,index);rendering=false;
  tips.SetToolTip(path,Texts.T("Choose steam_api64.dll from your game installation.","选择游戏安装目录中的 steam_api64.dll。","選擇遊戲安裝目錄中的 steam_api64.dll。"));status.Text=Texts.T("Choose your game library, then connect Steam to load achievement cards.","选择游戏库文件并连接 Steam，即可加载成就卡片。","選擇遊戲程式庫並連線 Steam，即可載入成就卡片。");UpdateSummary();
 }
 void UpdateSummary(){int count=items.Count(x=>x.Done);summary.Text=items.Count==0?Texts.T("66 achievements to discover","探索 66 项成就","探索 66 項成就"):count+" / "+items.Count+"  ·  "+Texts.T("unlocked","已解锁","已解鎖");account.Text=preview?Texts.T("PREVIEW · illustrative status","界面预览 · 示例状态","介面預覽 · 範例狀態"):session!=null&&session.Connected?"Steam ID  "+session.User:Texts.T("●  Not connected","●  尚未连接","●  尚未連線");progress.Invalidate();}
 void ClearItems(){foreach(var item in items)item.Dispose();items.Clear();chosen.Clear();}
 void Reload(){ClearItems();for(int i=1;i<=66;i++){string id="ACH_"+i.ToString("000");items.Add(new Achievement{Id=id,Name=session.Attribute(id,"name"),Description=session.Attribute(id,"desc"),Done=session.Read(id),Image=session.Icon(id)});}DrawCards();}
 void DrawCards(){if(rendering)return;rendering=true;cards.SuspendLayout();try{while(cards.Controls.Count>0){var c=cards.Controls[0];cards.Controls.RemoveAt(0);if(c!=empty)c.Dispose();}string query=search.Text.Trim();var visible=items.Where(x=>(filter.SelectedIndex!=1||!x.Done)&&(filter.SelectedIndex!=2||x.Done)&&(query.Length==0||(x.Id+" "+x.Name+" "+x.Description).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0));foreach(var item in visible){var card=new AchievementCard(item,async id=>await Submit(new List<string>{id}),()=>SelectionChanged());card.Selection.Checked=chosen.Contains(item.Id);cards.Controls.Add(card);}if(cards.Controls.Count==0){empty.Text=items.Count==0?Texts.T("Your achievement collection starts here.\nConnect Steam to load game artwork and progress.","你的成就图鉴，从这里开始。\n连接 Steam，载入游戏图标与完成进度。","你的成就圖鑑，從這裡開始。\n連線 Steam，載入遊戲圖示與完成進度。"):Texts.T("No matching achievements.","没有符合条件的成就。","沒有符合條件的成就。");cards.Controls.Add(empty);}SizeCards();}finally{cards.ResumeLayout();rendering=false;}UpdateSummary();EnableControls();}
 void SizeCards(){int width=Math.Max(300,cards.ClientSize.Width-SystemInformation.VerticalScrollBarWidth-4);int columns=width>=850?2:1;foreach(Control c in cards.Controls)c.Width=columns==2?(width-24)/2:width-12;}
 void SelectionChanged(){if(rendering)return;foreach(var card in cards.Controls.OfType<AchievementCard>()){if(card.Selection.Checked)chosen.Add(card.Item.Id);else chosen.Remove(card.Item.Id);}EnableControls();}
 void EnableControls(){bool ok=session!=null&&session.Connected&&!preview;connect.Enabled=browse.Enabled=path.Enabled=language.Enabled=!busy;search.Enabled=filter.Enabled=!busy;refresh.Enabled=ok&&!busy;selected.Enabled=ok&&!busy&&chosen.Count>0;all.Enabled=ok&&!busy&&items.Any(x=>!x.Done);selected.Text=Texts.T("Unlock selected","解锁勾选项","解鎖勾選項")+" ("+chosen.Count+")";foreach(var card in cards.Controls.OfType<AchievementCard>()){card.Unlock.Enabled=ok&&!busy&&!card.Item.Done;card.Selection.Enabled=!busy;}}
 async Task Run(Func<Task> action,string success){if(busy)return;busy=true;EnableControls();status.ForeColor=Theme.Gold;status.Text=Texts.T("Working… Waiting for Steam.","处理中… 正在等待 Steam。","處理中… 正在等待 Steam。");try{await action();status.ForeColor=Theme.Green;status.Text=success;}catch(Exception e){status.ForeColor=Color.Salmon;status.Text=e.Message;tips.SetToolTip(status,e.Message);if(session!=null){session.Dispose();session=null;}UpdateSummary();}finally{busy=false;EnableControls();}}
 async Task Submit(List<string> ids){if(preview||ids.Count==0)return;await Run(async()=>{await session.Unlock(ids);Reload();},Texts.T("Steam confirmed saving. Achievement status verified.","Steam 已确认保存，成就状态已验证。","Steam 已確認儲存，成就狀態已驗證。"));}
 public void Preview(int lang){preview=true;language.SelectedIndex=lang;items.Add(new Achievement{Id="ACH_038",Name="The Healer’s Legacy",Description="Complete [Flesh Lily], [Apprentice Healer], and [A Healer’s Mercy]",Done=false});items.Add(new Achievement{Id="ACH_066",Name="Defy Destiny",Description="Unlock all other achievements",Done=false});items.Add(new Achievement{Id="ACH_001",Name="The Heart’s Journey",Description="Escape the Pursuers on the Main Road",Done=true});items.Add(new Achievement{Id="ACH_064",Name="Lightning Fast",Description="Use Thunder Spear to paralyze an enemy",Done=true});DrawCards();}
 public void VerifyUi(){
  Preview(0);
  foreach(int lang in new[]{0,1,2}){
   language.SelectedIndex=lang;filter.SelectedIndex=0;search.Text="";
   if(cards.Controls.OfType<AchievementCard>().Count()!=4)throw new Exception("Initial cards");
   cards.Controls.OfType<AchievementCard>().First().Selection.Checked=true;
   filter.SelectedIndex=1;if(cards.Controls.OfType<AchievementCard>().Count()!=2)throw new Exception("Locked filter");
   search.Text="ACH_066";if(cards.Controls.OfType<AchievementCard>().Single().Item.Id!="ACH_066")throw new Exception("Search");
   if(!chosen.Contains("ACH_038"))throw new Exception("Selection lost across filters");
   search.Text="not-a-real-achievement";if(cards.Controls.OfType<AchievementCard>().Any())throw new Exception("Empty search");
   search.Text="";filter.SelectedIndex=0;Size=new Size(820,700);Application.DoEvents();
   foreach(var card in cards.Controls.OfType<AchievementCard>())if(card.Right>cards.ClientSize.Width||card.Unlock.Right>card.Width)throw new Exception("Narrow layout overflow");
   if(all.Enabled||selected.Enabled)throw new Exception("Preview must not allow writes");
  }
 }
 public async Task ReadOnlyPreview(string dll,int lang=0){await sessionConnect(dll);preview=true;language.SelectedIndex=lang;account.Text="Steam · read-only preview";status.Text=Texts.T("Read-only preview · no changes made.","只读预览 · 未修改成就。","唯讀預覽 · 未修改成就。");EnableControls();}
 async Task sessionConnect(string dll){session=new Session();await session.Connect(dll);Reload();for(int i=0;i<15&&items.Any(x=>x.Image==null);i++){session.Tick();foreach(var item in items.Where(x=>x.Image==null))item.Image=session.Icon(item.Id);await Task.Delay(200);}DrawCards();}
}
}

