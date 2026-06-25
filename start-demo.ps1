$root = $PSScriptRoot
Write-Host "Starting Marketplace Copilot demo..." -ForegroundColor Cyan

Get-Process MarketplaceCopilot.Api -ErrorAction SilentlyContinue | Stop-Process -Force
Get-NetTCPConnection -LocalPort 5280 -ErrorAction SilentlyContinue |
  ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }

# The API targets .NET 9. If the 9.0 runtime isn't installed, roll forward to a newer
# major (e.g. 10.0) so `dotnet run` still launches instead of failing with "must install .NET 9".
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root\MarketplaceCopilot.Api'; `$env:DOTNET_ROLL_FORWARD = 'Major'; Write-Host 'Backend API -> http://localhost:5280' -ForegroundColor Green; dotnet run --launch-profile http"
Start-Sleep -Seconds 4

Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root\frontend'; Write-Host 'Frontend -> http://localhost:4200' -ForegroundColor Green; npm start"
Write-Host ""
Write-Host "Demo starting in two windows." -ForegroundColor Green
Write-Host "  Frontend: http://localhost:4200" -ForegroundColor Yellow
Write-Host "  Backend:  http://localhost:5280" -ForegroundColor Yellow
Write-Host "  Login:    demo@marketplace.com / demo123" -ForegroundColor Yellow
Write-Host "  Or click 'Try Demo Login' on the sign-in page." -ForegroundColor Yellow
