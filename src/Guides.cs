using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace PBZAchievements {
static class Guides {
 public const string Url="https://steamcommunity.com/sharedfiles/filedetails/?id=3829337248";
 public static bool Missable(string id){return Array.IndexOf(new[]{"ACH_013","ACH_032","ACH_034","ACH_035","ACH_036","ACH_037","ACH_038"},id)>=0;}
 static string Hint(string id){
  switch(id){
   case "ACH_038":return Texts.T("Finish the herb quest before Jing Ye; the healing quest before Bai Chilian.","草药任务须在击败荆夜前完成；救治任务须在击败白赤练前完成。","草藥任務須在擊敗荊夜前完成；救治任務須在擊敗白赤練前完成。");
   case "ACH_034":return Texts.T("Accept the quest before Bai Chilian.","击败白赤练前接取任务。","擊敗白赤練前接取任務。");
   case "ACH_028":return Texts.T("Defeat You Loong in Meigu Lake.","需要击败媚骨湖版本的游龙帮主。","需要擊敗媚骨湖版本的游龍幫主。");
   case "ACH_013":return Texts.T("Finish the true-ending prerequisites first.","先完成真结局的前置任务。","先完成真結局的前置任務。");
   default:return Texts.T("See the original guide for the route and quest steps.","详细路线和任务步骤请查看原攻略。","詳細路線和任務步驟請查看原攻略。");
  }
 }
 public static void Show(IWin32Window owner,Achievement item,string previewPath=null){
  using(var dialog=new Form{Text=Texts.T("Achievement guide","成就攻略","成就攻略"),Size=new Size(660,520),MinimumSize=new Size(570,470),StartPosition=FormStartPosition.CenterParent,BackColor=Theme.Background,ForeColor=Theme.Text,Font=new Font("Segoe UI",10)}){
   var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(24),ColumnCount=1,RowCount=6};dialog.Controls.Add(root);
   foreach(int h in new[]{60,34})root.RowStyles.Add(new RowStyle(SizeType.Absolute,h));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,64));root.RowStyles.Add(new RowStyle(SizeType.Absolute,38));root.RowStyles.Add(new RowStyle(SizeType.Absolute,46));
   var title=Theme.Label(item.Name,19,Theme.Text);title.Dock=DockStyle.Fill;title.AutoEllipsis=true;root.Controls.Add(title);
   var badge=Theme.Label(Missable(item.Id)?Texts.T("MISSABLE · according to the guide","易错过 · 攻略标记","易錯過 · 攻略標記"):Texts.T("GAME REQUIREMENT & GUIDE","游戏条件与攻略提示","遊戲條件與攻略提示"),10,Theme.Gold);badge.Dock=DockStyle.Fill;root.Controls.Add(badge);
   var body=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,BackColor=Theme.Surface,ForeColor=Theme.Text,Text=item.Description+Environment.NewLine+Environment.NewLine+Hint(item.Id)};root.Controls.Add(body);
   var attribution=Theme.Label("Nero · Phantom Blade Zero 100% Achievements Guide\n"+Texts.T("Source updated Nov 1, 2026 · Work in progress · Spoilers","来源更新：2026-11-01 · 持续更新中 · 含剧透","來源更新：2026-11-01 · 持續更新中 · 含劇透"),9,Theme.Muted);attribution.Dock=DockStyle.Fill;root.Controls.Add(attribution);
   var note=Theme.Label(Texts.T("Guide notes describe gameplay; they do not change Steam unlock behavior.","攻略提示用于正常游玩，不改变 Steam 解锁逻辑。","攻略提示用於正常遊玩，不改變 Steam 解鎖邏輯。"),9,Theme.Muted);note.Dock=DockStyle.Fill;root.Controls.Add(note);
   var open=Theme.Button(Texts.T("Open original Steam guide","打开 Steam 原攻略","開啟 Steam 原攻略"),true);open.Width=280;open.Click+=(s,e)=>{try{Process.Start(new ProcessStartInfo(Url){UseShellExecute=true});}catch(Exception error){MessageBox.Show(dialog,error.Message);}};root.Controls.Add(open);
   if(previewPath!=null)dialog.Shown+=async(s,e)=>{await System.Threading.Tasks.Task.Delay(300);using(var bitmap=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(bitmap,new Rectangle(0,0,bitmap.Width,bitmap.Height));bitmap.Save(previewPath);}dialog.Close();};
   dialog.ShowDialog(owner);
  }
 }
}
}
