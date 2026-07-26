#!/usr/bin/env bash
# build_switch.sh — Build The Signal for Nintendo Switch
# Requires: Godot 4.3+ with .NET 8, NVN SDK, devkitpro

set -euo pipefail

PROJECT_DIR="G:/games_i_created/TheSignal"
BUILD_DIR="${PROJECT_DIR}/build/switch"
EXPORT_PRESET="switch"
VERSION=$(grep 'config/version' "${PROJECT_DIR}/project.godot" | cut -d'"' -f2)

echo "=== The Signal — Nintendo Switch Build ==="
echo "Version: ${VERSION}"
echo ""

dotnet restore "${PROJECT_DIR}/TheSignal.csproj"
dotnet build "${PROJECT_DIR}/TheSignal.csproj" -c Release

echo "[Exporting Switch .nsp...]"
mkdir -p "${BUILD_DIR}"
godot --headless --export-release "${EXPORT_PRESET}" "${BUILD_DIR}/TheSignal.nsp"

echo "[Running NX certification...]"
python3 "${PROJECT_DIR}/Content/Certification/run_cert_check.py" --platform switch --build "${BUILD_DIR}"

echo "[Packaging...]"
cd "${BUILD_DIR}"
zip -r "TheSignal_Switch_${VERSION}.zip" ./*
echo "Package: ${BUILD_DIR}/TheSignal_Switch_${VERSION}.zip"