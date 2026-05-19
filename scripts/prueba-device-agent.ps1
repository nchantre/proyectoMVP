# Prueba rápida: API + ingesta dispositivo (sin levantar el DeviceAgent completo).
$apiUrl = "http://localhost:5290"
$deviceId = "11111111-1111-1111-1111-111111111111"
$apiKey = "DEMO_KEY"

Write-Host ""
Write-Host "=== Prueba DeviceAgent / ingesta ===" -ForegroundColor Cyan
Write-Host ""

# 1) ¿Está la API arriba?
Write-Host "[1/4] Comprobando API en $apiUrl ..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$apiUrl/api/health" -Method Get -TimeoutSec 5
    Write-Host "      OK - API respondiendo." -ForegroundColor Green
}
catch {
    Write-Host "      FALLO - La API no está en marcha." -ForegroundColor Red
    Write-Host ""
    Write-Host "  Abre PowerShell y ejecuta:" -ForegroundColor White
    Write-Host "    cd d:\ProyectoMVP\ProyectoMVP\proyectoMVP" -ForegroundColor Gray
    Write-Host "    .\scripts\run-dev.ps1" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Espera a que abran 2 ventanas (API y Web). Vuelve a ejecutar este script." -ForegroundColor White
    exit 1
}

# 2) Enviar un avistamiento como si fuera el dispositivo
Write-Host "[2/4] Enviando 1 lectura LPR simulada (batch) ..." -ForegroundColor Yellow
$plate = "TST" + (Get-Random -Maximum 9999).ToString("0000")
$body = @{
    items = @(
        @{
            plate       = $plate
            seenAtUtc   = (Get-Date).ToUniversalTime().ToString("o")
            latitude    = 6.2442
            longitude   = -75.5812
            confidence  = 95.5
            evidenceUrl = "https://storage.demo/evidence/${plate}_test.jpg"
            evidenceType = "IMAGE"
        }
    )
} | ConvertTo-Json -Depth 5

try {
    $result = Invoke-RestMethod `
        -Uri "$apiUrl/api/v1/devices/$deviceId/sightings/batch" `
        -Method Post `
        -Headers @{ "X-Api-Key" = $apiKey } `
        -ContentType "application/json" `
        -Body $body

    Write-Host "      OK - Placa $plate : $($result.accepted) aceptado(s), $($result.duplicates) duplicado(s)." -ForegroundColor Green
}
catch {
    Write-Host "      FALLO - $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) { Write-Host "      $($_.ErrorDetails.Message)" -ForegroundColor Red }
    exit 1
}

# 3) Login policía y consultar la placa
Write-Host "[3/4] Consultando placa $plate como policía ..." -ForegroundColor Yellow
try {
    $login = Invoke-RestMethod -Uri "$apiUrl/api/v1/auth/login" -Method Post `
        -ContentType "application/json" `
        -Body '{"username":"policia","password":"demo"}'

    $token = $login.accessToken
    $sightings = Invoke-RestMethod -Uri "$apiUrl/api/v1/plates/$plate/sightings" -Method Get `
        -Headers @{ Authorization = "Bearer $token" }

    $count = @($sightings.sightings).Count
    Write-Host "      OK - $count avistamiento(s) para $plate" -ForegroundColor Green
}
catch {
    Write-Host "      AVISO - No se pudo consultar (revisa BD/seed): $($_.Exception.Message)" -ForegroundColor Yellow
}

# 4) Instrucciones DeviceAgent continuo
Write-Host "[4/4] Siguiente paso (agente automático)" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Para que el dispositivo envíe lecturas cada ~25 s:" -ForegroundColor White
Write-Host "    1. Deja la API corriendo (run-dev.ps1)" -ForegroundColor Gray
Write-Host "    2. Abre OTRA ventana PowerShell:" -ForegroundColor Gray
Write-Host "         cd d:\ProyectoMVP\ProyectoMVP\proyectoMVP" -ForegroundColor Gray
Write-Host "         .\scripts\run-device-agent.ps1" -ForegroundColor Gray
Write-Host "    3. Verás logs: LPR -> cola local -> Sync OK" -ForegroundColor Gray
Write-Host "    4. En el mapa http://localhost:5103 busca ABC123 (login policia/demo)" -ForegroundColor Gray
Write-Host ""
Write-Host "  Demo sin internet: en appsettings.json del DeviceAgent pon SyncEnabled: false" -ForegroundColor DarkGray
Write-Host ""
