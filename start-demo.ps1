$root = $PSScriptRoot
Write-Host "Starting Marketplace Copilot demo..." -ForegroundColor Cyan

Get-Process MarketplaceCopilot.Api -ErrorAction SilentlyContinue | Stop-Process -Force
Get-NetTCPConnection -LocalPort 5280 -ErrorAction SilentlyContinue |
  ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }

Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root\MarketplaceCopilot.Api'; Write-Host 'Backend API -> http://localhost:5280' -ForegroundColor Green; dotnet run --launch-profile http"
Start-Sleep -Seconds 4

Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root\frontend'; Write-Host 'Frontend -> http://localhost:4200' -ForegroundColor Green; npm start"
Write-Host ""
Write-Host "Demo starting in two windows." -ForegroundColor Green
Write-Host "  Frontend: http://localhost:4200" -ForegroundColor Yellow
Write-Host "  Backend:  http://localhost:5280" -ForegroundColor Yellow
Write-Host "  Login:    demo@marketplace.com / demo123" -ForegroundColor Yellow
Write-Host "  Or click 'Try Demo Login' on the sign-in page." -ForegroundColor Yellow
