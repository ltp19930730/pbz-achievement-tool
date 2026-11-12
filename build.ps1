$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
New-Item -ItemType Directory -Force (Join-Path $root 'dist') | Out-Null
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /r:System.Windows.Forms.dll /r:System.Drawing.dll ("/out:" + (Join-Path $root 'dist\PBZAchievementTool.exe')) (Join-Path $root 'src\Program.cs') (Join-Path $root 'src\Cards.cs') (Join-Path $root 'src\Guides.cs')
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
Write-Output (Join-Path $root 'dist\PBZAchievementTool.exe')
