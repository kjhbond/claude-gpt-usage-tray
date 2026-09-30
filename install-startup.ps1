param([string]$PythonPath)
$ErrorActionPreference='Stop'
Set-Location $PSScriptRoot
$python=if($PythonPath){$PythonPath}else{python -c 'import sys; print(sys.executable)'}
if (!(Test-Path -LiteralPath $python)) {throw 'Python executable missing'}
$pythonw=Join-Path (Split-Path $python) 'pythonw.exe'
if (!(Test-Path $pythonw)) {throw 'Python windowless executable missing'}
[IO.File]::WriteAllText("$PSScriptRoot\pythonw-path.txt",$pythonw)
if (!(Test-Path -LiteralPath "$PSScriptRoot\CodexQuotaWidget.exe")) {
 & "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /r:System.Management.dll /out:CodexQuotaWidget.exe Launcher.cs
 if($LASTEXITCODE -ne 0){throw 'Launcher compilation failed'}
}
$run='HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
New-ItemProperty -Path $run -Name CodexWeeklyQuotaWidget -Value ('"'+$PSScriptRoot+'\CodexQuotaWidget.exe"') -PropertyType String -Force | Out-Null
Get-ItemPropertyValue $run CodexWeeklyQuotaWidget
$launcher=Join-Path $PSScriptRoot 'CodexQuotaWidget.exe'
$result=Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{
 CommandLine='"'+$launcher+'"'
 CurrentDirectory=$PSScriptRoot
}
if($result.ReturnValue -ne 0){throw "Detached widget launch failed: $($result.ReturnValue)"}
