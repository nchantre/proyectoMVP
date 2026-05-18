# Detiene procesos que bloquean compilación o puertos de desarrollo.
$ports = @(5290, 5103)
$stopped = [System.Collections.Generic.HashSet[int]]::new()

Write-Host "Deteniendo ProyectMVP.Api y ProyectMVP.Web..." -ForegroundColor Yellow
Get-Process -Name "ProyectMVP.Api", "ProyectMVP.Web" -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "  $($_.ProcessName) -> PID $($_.Id)"
    Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    [void]$stopped.Add($_.Id)
}

Write-Host "Liberando puertos: $($ports -join ', ')..." -ForegroundColor Yellow
foreach ($port in $ports) {
    $connections = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    foreach ($conn in $connections) {
        $processId = $conn.OwningProcess
        if ($processId -and $processId -ne 0 -and $stopped.Add($processId)) {
            $name = (Get-Process -Id $processId -ErrorAction SilentlyContinue).ProcessName
            Write-Host "  Puerto $port -> PID $processId ($name)"
            Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
        }
    }
}

Start-Sleep -Seconds 1
Write-Host "Listo. Ahora puedes ejecutar: dotnet build ProyectMVP.sln" -ForegroundColor Green
