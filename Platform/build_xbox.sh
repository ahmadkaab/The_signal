#!/usr/bin/env bash
# build_xbox.sh — Build The Signal for Xbox Series X|S
# Requires: Godot 4.3+ with .NET 8, Xbox GDK, MSVC build tools

set -euo pipefail

PROJECT_DIR="G:/games_i_created/TheSignal"
BUILD_DIR="${PROJECT_DIR}/build/xbox"
EXPORT_PRESET="xbox_series"
VERSION=$(grep 'config/version' "${PROJECT_DIR}/project.godot" | cut -d'"' -f2)

echo "=== The Signal — Xbox Series X|S Build ==="
echo "Version: ${VERSION}"
echo "Output: ${BUILD_DIR}"
echo ""

# 1. Restore NuGet packages
echo "[1/5] Restoring NuGet packages..."
cd "${PROJECT_DIR}"
dotnet restore TheSignal.csproj

# 2. Build C# solution
echo "[2/5] Building C# assemblies..."
dotnet build TheSignal.csproj -c Release

# 3. Export via Godot headless
echo "[3/5] Exporting game..."
mkdir -p "${BUILD_DIR}"
godot --headless --export-release "${EXPORT_PRESET}" "${BUILD_DIR}/TheSignal.xvc"

# 4. Run certification checks
echo "[4/5] Running TCR certification..."
python3 "${PROJECT_DIR}/Content/Certification/run_cert_check.py" --platform xbox --build "${BUILD_DIR}"

# 5. Package for submission
echo "[5/5] Packaging submission..."
cd "${BUILD_DIR}"
zip -r "TheSignal_Xbox_${VERSION}.zip" ./*
echo ""
echo "=== Build Complete ==="
echo "Package: ${BUILD_DIR}/TheSignal_Xbox_${VERSION}.zip"