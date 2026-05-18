# Inicia API y Web en ventanas separadas con puertos fijos.
$root = Split-Path $PSScriptRoot -Parent

Write-Host "Puertos: API=http://localhost:5290  Web=http://localhost:5103" -ForegroundColor Cyan
Write-Host "Deteniendo instancias previas..."
& "$PSScriptRoot\stop-dev.ps1"

Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "cd '$root'; dotnet run --project src/ProyectMVP.Api --launch-profile http"
)

Start-Sleep -Seconds 3

Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "cd '$root'; dotnet run --project src/ProyectMVP.Web --launch-profile http"
)

Write-Host ""
Write-Host "  API (Swagger): http://localhost:5290/swagger" -ForegroundColor Green
Write-Host "  Web (Mapa):    http://localhost:5103" -ForegroundColor Green
