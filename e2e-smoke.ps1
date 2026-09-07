# e2e-smoke.ps1 - Real-process smoke test for DotNetCoreRpc.
# Starts Test.Server6 (real Kestrel on a real TCP port), then runs Test.Client
# against it, and reports PASS/FAIL. Requires net9.0 runtime.

[CmdletBinding()]
param(
    [string]$Root = ''
)
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Root)) { $Root = Split-Path -Parent $MyInvocation.MyCommand.Path }

#统一控制台输出为 UTF-8，避免重定向/管道时中文乱码
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }

$port = 34047
$serverCsproj = Join-Path $Root 'demo\Test.Server6\Test.Server6.csproj'
$clientCsproj = Join-Path $Root 'demo\Test.Client\Test.Client.csproj'
$serverExe    = Join-Path $Root 'demo\Test.Server6\bin\Debug\net9.0\Test.Server6.exe'
$clientExe    = Join-Path $Root 'demo\Test.Client\bin\Debug\net9.0\Test.Client.exe'

$work = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), 'dncrpc-e2e-' + [System.Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
$srvOut = Join-Path $work 'server.out.log'
$srvErr = Join-Path $work 'server.err.log'
$cOut   = Join-Path $work 'client.out.log'
$cErr   = Join-Path $work 'client.err.log'
$cIn    = Join-Path $work 'client.in.txt'

try {
    Write-Host '==> Build Test.Server6'
    & dotnet build $serverCsproj --nologo -v minimal | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Server build failed' }

    Write-Host '==> Build Test.Client'
    & dotnet build $clientCsproj --nologo -v minimal | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Client build failed' }

    Write-Host '==> Start Test.Server6'
    $serverDir = Split-Path -Parent $serverCsproj
    $serverDll = Join-Path $serverDir "bin\Debug\net9.0\Test.Server6.dll"
    $srv = Start-Process -FilePath 'dotnet' `
        -ArgumentList @($serverDll, '--urls', ("http://0.0.0.0:$port")) `
        -WorkingDirectory $serverDir -PassThru `
        -RedirectStandardOutput $srvOut -RedirectStandardError $srvErr

    Write-Host '==> Wait for server to listen...'
    Start-Sleep -Seconds 1
    if ($srv.HasExited) {
        Write-Host '--- server.err.log ---'
        if (Test-Path $srvErr) { Get-Content $srvErr -Encoding UTF8 }
        Write-Host '--- server.out.log ---'
        if (Test-Path $srvOut) { Get-Content $srvOut -Encoding UTF8 }
        throw "Server process exited early with code $($srv.ExitCode)"
    }

    $ready = $false
    for ($i = 0; $i -lt 40; $i++) {
        try {
            $tcp = New-Object System.Net.Sockets.TcpClient
            $iar = $tcp.BeginConnect('localhost', $port, $null, $null)
            if ($iar.AsyncWaitHandle.WaitOne(1000)) {
                $tcp.EndConnect($iar); $ready = $true; $tcp.Close(); break
            }
            $tcp.Close()
        } catch { }
        if (-not $ready) { Start-Sleep -Milliseconds 500 }
    }
    if (-not $ready) {
        Write-Host '--- server.err.log ---'
        if (Test-Path $srvErr) { Get-Content $srvErr -Encoding UTF8 }
        Write-Host '--- server.out.log ---'
        if (Test-Path $srvOut) { Get-Content $srvOut -Encoding UTF8 }
        throw 'Server did not start in time'
    }

    Write-Host '==> Run Test.Client'
    Set-Content -Path $cIn -Value "`r`n`r`n`r`n" -NoNewline
    $cl = Start-Process -FilePath $clientExe -PassThru `
        -RedirectStandardInput $cIn -RedirectStandardOutput $cOut -RedirectStandardError $cErr -Wait

    if (Test-Path $cOut) { Get-Content $cOut -Encoding UTF8 | ForEach-Object { Write-Host ($_ -replace '^(.*)$', '   $1') } }
    if (-not $cl.HasExited) { Wait-Process -Id $cl.Id -Timeout 30 }
    if ($cl.ExitCode -ne 0) { throw "Client exited with code $($cl.ExitCode)" }

    Write-Host '==> PASS (client exit 0): real-process end-to-end round-trip OK'
}
finally {
    if ($srv -and -not $srv.HasExited) {
        Write-Host '==> Stopping server'
        Stop-Process -Id $srv.Id -Force
    }
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}