Set-Location $PSScriptRoot

$env:AndroidSigningKeyPass   = "Nx7@mK2pRw9#vQ4s"
$env:AndroidSigningStorePass = "Nx7@mK2pRw9#vQ4s"

Write-Host "Cleaning..." -ForegroundColor Cyan
Remove-Item -Recurse -Force bin\Release\net9.0-android, obj\Release\net9.0-android -ErrorAction SilentlyContinue

Write-Host "Building AAB..." -ForegroundColor Cyan
dotnet publish -f net9.0-android -c Release -p:AndroidPackageFormat=aab

if ($LASTEXITCODE -ne 0) { Write-Host "Build failed." -ForegroundColor Red; exit 1 }

$aab = "bin\Release\net9.0-android\publish\com.nooria.app-Signed.aab"
$size = (Get-Item $aab).Length / 1MB
Write-Host ""
Write-Host "Done! Upload this to Play Store:" -ForegroundColor Green
Write-Host "  $PSScriptRoot\$aab ($([math]::Round($size, 1)) MB)" -ForegroundColor Yellow
