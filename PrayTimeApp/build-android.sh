#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

# Pass password via env vars to avoid special character parsing issues
export AndroidSigningKeyPass="Nx7@mK2pRw9#vQ4s"
export AndroidSigningStorePass="Nx7@mK2pRw9#vQ4s"

echo "Cleaning..."
rm -rf bin/Release/net9.0-android obj/Release/net9.0-android

echo "Building AAB..."
dotnet publish -f net9.0-android -c Release -p:AndroidPackageFormat=aab

AAB="bin/Release/net9.0-android/publish/com.nooria.app-Signed.aab"
VERSION=$(grep -oP 'versionName="\K[^"]+' obj/Release/net9.0-android/android/AndroidManifest.xml)
SIZE=$(du -sh "$AAB" | cut -f1)

echo ""
echo "Done"
echo "  Version : $VERSION"
echo "  Size    : $SIZE"
echo "  File    : $SCRIPT_DIR/$AAB"
