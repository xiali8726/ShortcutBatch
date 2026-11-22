$ErrorActionPreference = 'Stop'

# 定位系统自带的 .NET Framework 4.x 编译器（优先 64 位）
$fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$csc = Join-Path $fw 'csc.exe'
if (-not (Test-Path -LiteralPath $csc)) {
    $fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
    $csc = Join-Path $fw 'csc.exe'
}
if (-not (Test-Path -LiteralPath $csc)) { throw '未找到 csc.exe (.NET Framework 4)' }

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$out  = Join-Path $root 'ShortcutBatch.exe'

& $csc /nologo /codepage:65001 /optimize+ /target:exe "/out:$out" `
    "/win32manifest:$root\app.manifest" `
    "/r:$fw\System.dll" `
    "/r:$fw\System.Core.dll" `
    "/r:$fw\System.Drawing.dll" `
    "/r:$fw\System.Windows.Forms.dll" `
    "/r:$fw\Microsoft.CSharp.dll" `
    (Join-Path $root 'Program.cs') `
    (Join-Path $root 'ShortcutCreator.cs') `
    (Join-Path $root 'Settings.cs') `
    (Join-Path $root 'MainForm.cs')

if ($LASTEXITCODE -ne 0) { throw "编译失败，退出码 $LASTEXITCODE" }
Write-Host "编译成功: $out"
