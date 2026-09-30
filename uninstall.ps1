$ErrorActionPreference='Stop'
Remove-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name CodexWeeklyQuotaWidget -ErrorAction SilentlyContinue
& "$PSScriptRoot\Windhawk\windhawk.exe" -exit -wait
Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'FallbackTray.exe' -and $_.ExecutablePath -eq "$PSScriptRoot\FallbackTray.exe" } | ForEach-Object { Stop-Process -Id $_.ProcessId -ErrorAction SilentlyContinue }
Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'pythonw.exe' -and ($_.CommandLine -like ('*'+$PSScriptRoot+'\quota.py*') -or $_.CommandLine -like ('*'+$PSScriptRoot+'\claude_quota.py*')) } | ForEach-Object { Stop-Process -Id $_.ProcessId -ErrorAction SilentlyContinue }
Write-Output 'Widget stopped and login startup removed. Source files retained.'
