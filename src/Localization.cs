using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
namespace PBZAchievements {
static partial class Texts {
 public static readonly object[] Names={"English","简体中文","繁體中文","Français","Deutsch","Español (España)","Русский","日本語","Português (Brasil)","한국어","Italiano","ไทย","Tiếng Việt","Português (Portugal)","Español (Latinoamérica)"};
 static readonly int[] Columns={0,0,0,1,2,3,4,5,6,7,8,9,10,6,3};
 static readonly Dictionary<string,string[]> Catalog=Load();
 static Dictionary<string,string[]> Load(){var result=new Dictionary<string,string[]>();using(var reader=new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("PBZ.Locales"))){string line;while((line=reader.ReadLine())!=null){if(string.IsNullOrWhiteSpace(line))continue;var parts=line.Split('|');if(parts.Length!=11)throw new InvalidDataException("Invalid locale row: "+parts[0]);result.Add(parts[0],parts);}}return result;}
 public static string Localize(string en){if(en.StartsWith("Your achievement collection starts here."))en="Choose your game library, then connect Steam to load achievement cards.";string[] row;return Language>=3&&Language<Columns.Length&&Catalog.TryGetValue(en,out row)?row[Columns[Language]]:en;}
 public static int SystemLanguage(){string c=System.Globalization.CultureInfo.CurrentUICulture.Name.ToLowerInvariant();if(c.StartsWith("zh"))return c.Contains("tw")||c.Contains("hk")||c.Contains("hant")?2:1;if(c.StartsWith("fr"))return 3;if(c.StartsWith("de"))return 4;if(c.StartsWith("es"))return c=="es-es"?5:14;if(c.StartsWith("ru"))return 6;if(c.StartsWith("ja"))return 7;if(c.StartsWith("pt"))return c=="pt-pt"?13:8;if(c.StartsWith("ko"))return 9;if(c.StartsWith("it"))return 10;if(c.StartsWith("th"))return 11;if(c.StartsWith("vi"))return 12;return 0;}
}
}
