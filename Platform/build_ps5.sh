#!/usr/bin/env bash
# build_ps5.sh — Build The Signal for PlayStation 5
# Requires: Godot 4.3+ with .NET 8, PS5 SDK, clang for Orbis

set -euo pipefail

PROJECT_DIR="G:/games_i_created/TheSignal"
BUILD_DIR="${PROJECT_DIR}/build/ps5"
EXPORT_PRESET="ps5"
VERSION=$(grep 'config/version' "${PROJECT_DIR}/project.godot" | cut -d'"' -f2)

echo "=== The Signal — PlayStation 5 Build ==="
echo "Version: ${VERSION}"
echo ""

dotnet restore "${PROJECT_DIR}/TheSignal.csproj"
dotnet build "${PROJECT_DIR}/TheSignal.csproj" -c Release

echo "[Exporting PS5 .pkg...]"
mkdir -p "${BUILD_DIR}"
godot --headless --export-release "${EXPORT_PRESET}" "${BUILD_DIR}/TheSignal.pkg"

echo "[Running PS5 TRC certification...]"
python3 "${PROJECT_DIR}/Content/Certification/run_cert_check.py" --platform ps5 --build "${BUILD_DIR}"

echo "[Packaging...]"
cd "${BUILD_DIR}"
zip -r "TheSignal_PS5_${VERSION}.zip" ./*
echo "Package: ${BUILD_DIR}/TheSignal_PS5_${VERSION}.zip"