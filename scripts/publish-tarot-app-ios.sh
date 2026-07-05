#!/usr/bin/env bash
# ponytail: iOS 打包需在 macOS 上运行；本脚本仅生成 publish 目录供 Xcode 归档。
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
APP="$ROOT/src/IChing.Tarot.App"
CONFIG="${1:-Release}"
TFM="net10.0-ios"
RID="ios-arm64"

echo "==> Publishing IChing.Tarot.App for iOS ($CONFIG)"
cd "$APP"

EXTRA=()
if [[ -n "${IOS_CODESIGN_KEY:-}" ]]; then
  EXTRA+=(-p:CodesignKey="$IOS_CODESIGN_KEY")
fi
if [[ -n "${IOS_PROVISION_PROFILE:-}" ]]; then
  EXTRA+=(-p:CodesignProvision="$IOS_PROVISION_PROFILE")
fi

dotnet publish -f "$TFM" -c "$CONFIG" -p:RuntimeIdentifier="$RID" "${EXTRA[@]}"

echo "==> Output under bin/$CONFIG/$TFM/$RID/publish/"
echo "    Open Xcode Organizer to archive and upload to TestFlight."
