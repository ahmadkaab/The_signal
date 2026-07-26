#!/usr/bin/env bash
# build_all.sh — Build The Signal for all platforms (PC + console stubs)
# Run from project root.

set -euo pipefail

PROJECT_DIR="G:/games_i_created/TheSignal"
VERSION=$(grep 'config/version' "${PROJECT_DIR}/project.godot" | cut -d'"' -f2)
BUILD_DATE=$(date +%Y%m%d)

echo "========================================"
echo "  The Signal — Full Build Pipeline"
echo "  Version ${VERSION} (${BUILD_DATE})"
echo "========================================"
echo ""

# NuGet restore
echo "[0] Restoring packages..."
dotnet restore "${PROJECT_DIR}/TheSignal.csproj"

# PC build
echo ""
echo "--- Windows ---"
godot --headless --export-release "windows" "${PROJECT_DIR}/build/pc/TheSignal_${VERSION}.exe"

echo ""
echo "--- Linux ---"
godot --headless --export-release "linux" "${PROJECT_DIR}/build/pc/TheSignal_${VERSION}.x86_64"

echo ""
echo "--- Mac ---"
godot --headless --export-release "macos" "${PROJECT_DIR}/build/pc/TheSignal_${VERSION}.dmg"

# Console builds (if SDKs available)
echo ""
echo "--- Xbox Series X|S ---"
if command -v godot-xbox &>/dev/null; then
    bash "${PROJECT_DIR}/Platform/build_xbox.sh"
else
    echo "  [SKIP] Xbox GDK not detected"
fi

echo ""
echo "--- PlayStation 5 ---"
if command -v godot-ps5 &>/dev/null; then
    bash "${PROJECT_DIR}/Platform/build_ps5.sh"
else
    echo "  [SKIP] PS5 SDK not detected"
fi

echo ""
echo "--- Nintendo Switch ---"
if command -v godot-switch &>/dev/null; then
    bash "${PROJECT_DIR}/Platform/build_switch.sh"
else
    echo "  [SKIP] Switch SDK not detected"
fi

echo ""
echo "========================================"
echo "  Build pipeline complete."
echo "========================================"