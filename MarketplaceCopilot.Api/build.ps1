# Stop a running API instance so MSBuild can overwrite bin output (fixes MSB3027).
Get-Process MarketplaceCopilot.Api -ErrorAction SilentlyContinue | Stop-Process -Force
Get-NetTCPConnection -LocalPort 5280 -ErrorAction SilentlyContinue |
  ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }
Start-Sleep -Seconds 1
dotnet build @args
exit $LASTEXITCODE
