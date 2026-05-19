# Inicia el agente de dispositivo (simula LPR + cola SQLite + sync API).
# Requiere que ProyectMVP.Api esté en http://localhost:5290
$root = Split-Path $PSScriptRoot -Parent

Write-Host "DeviceAgent → API http://localhost:5290" -ForegroundColor Cyan
Write-Host "Ctrl+C para detener." -ForegroundColor Gray
Write-Host ""

Set-Location $root
dotnet run --project src/ProyectMVP.DeviceAgent
