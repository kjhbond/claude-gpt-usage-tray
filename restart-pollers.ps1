$ErrorActionPreference='Stop'
$processes=Get-CimInstance Win32_Process
$targets=@($processes | Where-Object {$_.Name -eq 'pythonw.exe' -and ($_.CommandLine -like ('*'+$PSScriptRoot+'\quota.py*') -or $_.CommandLine -like ('*'+$PSScriptRoot+'\claude_quota.py*'))})
foreach($p in $targets){
 $processes | Where-Object {$_.ParentProcessId -eq $p.ProcessId -and $_.Name -eq 'codex.exe'} | ForEach-Object {Stop-Process -Id $_.ProcessId -ErrorAction SilentlyContinue}
 Stop-Process -Id $p.ProcessId -ErrorAction SilentlyContinue
}
Start-Sleep -Milliseconds 400
Start-Process "$PSScriptRoot\CodexQuotaWidget.exe" -WindowStyle Hidden
