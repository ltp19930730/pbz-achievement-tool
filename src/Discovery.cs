using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace PBZAchievements {
static class Discovery {
 static string Value(string text,string key){var m=Regex.Match(text,"\""+Regex.Escape(key)+"\"\\s*\"([^\"]+)\"");return m.Success?m.Groups[1].Value.Replace("\\\\","\\"):null;}
 public static string FindLibrary(){
  var roots=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam")){if(key!=null){var path=key.GetValue("SteamPath") as string;if(!string.IsNullOrWhiteSpace(path))roots.Add(path);}}
  roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Steam"));
  foreach(var root in new List<string>(roots)){
   foreach(var file in new[]{Path.Combine(root,"steamapps","libraryfolders.vdf"),Path.Combine(root,"config","libraryfolders.vdf")}){
    try{if(File.Exists(file))foreach(Match m in Regex.Matches(File.ReadAllText(file),"\"(?:path|[0-9]+)\"\\s*\"([^\"]+)\"")){string p=m.Groups[1].Value.Replace("\\\\","\\");if(Path.IsPathRooted(p))roots.Add(p);}}catch(IOException){}catch(UnauthorizedAccessException){}
   }
  }
  foreach(var root in roots){try{
   string manifest=Path.Combine(root,"steamapps","appmanifest_4115450.acf");if(!File.Exists(manifest))continue;
   string install=Value(File.ReadAllText(manifest),"installdir");if(string.IsNullOrWhiteSpace(install)||Path.IsPathRooted(install)||install.Contains(".."))continue;
   string game=Path.Combine(root,"steamapps","common",install);
   foreach(string relative in new[]{"steam_api64.dll",@"steam\steam_api64.dll",@"platform\steam\steam_api64.dll"}){string dll=Path.Combine(game,relative);if(File.Exists(dll))return dll;}
  }catch(IOException){}catch(UnauthorizedAccessException){}}
  return null;
 }
}
}
