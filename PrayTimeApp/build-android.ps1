Set-Location $PSScriptRoot

# Pass password via env vars to avoid special character parsing issues
$env:AndroidSigningKeyPass   = "Nx7@mK2pRw9#vQ4s"
$env:AndroidSigningStorePass = "Nx7@mK2pRw9#vQ4s"

Write-Host "Cleaning..." -ForegroundColor Cyan
Remove-Item -Recurse -Force bin\Release\net9.0-android, obj\Release\net9.0-android -ErrorAction SilentlyContinue

Write-Host "Building APK..." -ForegroundColor Cyan
dotnet publish -f net9.0-android -c Release -p:AndroidPackageFormat=apk

if ($LASTEXITCODE -ne 0) { Write-Host "Build failed." -ForegroundColor Red; exit 1 }

Write-Host "Done: bin\Release\net9.0-android\publish\com.nooria.app-Signed.apk" -ForegroundColor Green
