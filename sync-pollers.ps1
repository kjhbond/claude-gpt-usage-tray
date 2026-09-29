$ErrorActionPreference='Stop'
$layout=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'layout.ini') -Encoding Unicode -Raw
$showCodex=$layout -notmatch '(?m)^ShowCodex=0\s*$'
$showClaude=$layout -notmatch '(?m)^ShowClaude=0\s*$'
if (-not $showCodex -and -not $showClaude) {$showCodex=$true}

$all=Get-CimInstance Win32_Process
$codex=@($all | Where-Object {$_.Name -eq 'pythonw.exe' -and $_.CommandLine -like ('*'+$PSScriptRoot+'\quota.py*')})
$claude=@($all | Where-Object {$_.Name -eq 'pythonw.exe' -and $_.CommandLine -like ('*'+$PSScriptRoot+'\claude_quota.py*')})

if (-not $showCodex) {
    foreach($p in $codex){
        $all | Where-Object {$_.Name -eq 'codex.exe' -and $_.ParentProcessId -eq $p.ProcessId} |
            ForEach-Object {Stop-Process -Id $_.ProcessId -ErrorAction SilentlyContinue}
        Stop-Process -Id $p.ProcessId -ErrorAction SilentlyContinue
    }
}
if (-not $showClaude) {
    foreach($p in $claude){Stop-Process -Id $p.ProcessId -ErrorAction SilentlyContinue}
}

if (($showCodex -and $codex.Count -eq 0) -or ($showClaude -and $claude.Count -eq 0)) {
    $pythonw=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'pythonw-path.txt')).Trim()
    if (!(Test-Path -LiteralPath $pythonw)) {throw 'pythonw.exe is missing'}
    if ($showCodex -and $codex.Count -eq 0) {
        Start-Process -FilePath $pythonw -ArgumentList ('"'+(Join-Path $PSScriptRoot 'quota.py')+'"') -WorkingDirectory $PSScriptRoot -WindowStyle Hidden
    }
    if ($showClaude -and $claude.Count -eq 0) {
        Start-Process -FilePath $pythonw -ArgumentList ('"'+(Join-Path $PSScriptRoot 'claude_quota.py')+'"') -WorkingDirectory $PSScriptRoot -WindowStyle Hidden
    }
}
