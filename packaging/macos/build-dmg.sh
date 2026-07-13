#!/usr/bin/env bash
set -euo pipefail

publish_dir="$(cd "$1" && pwd)"
output_dir="$(mkdir -p "$2" && cd "$2" && pwd)"
version="${3:-0.2.0}"
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/../.." && pwd)"
app="${output_dir}/MeshCommander Enhanced.app"
staging="${output_dir}/dmg-staging"

rm -rf "${app}" "${staging}"
mkdir -p "${app}/Contents/MacOS" "${app}/Contents/Resources" "${staging}"
cp -a "${publish_dir}/." "${app}/Contents/MacOS/"
sed "s/@VERSION@/${version}/g" "${script_dir}/Info.plist.in" > "${app}/Contents/Info.plist"

iconset="${output_dir}/MeshCommanderEnhanced.iconset"
mkdir -p "${iconset}"
sips -s format png "${repo_root}/favicon.ico" --out "${output_dir}/icon-source.png" >/dev/null
for size in 16 32 128 256 512; do
  sips -z "${size}" "${size}" "${output_dir}/icon-source.png" --out "${iconset}/icon_${size}x${size}.png" >/dev/null
  double=$((size * 2))
  sips -z "${double}" "${double}" "${output_dir}/icon-source.png" --out "${iconset}/icon_${size}x${size}@2x.png" >/dev/null
done
iconutil -c icns "${iconset}" -o "${app}/Contents/Resources/MeshCommanderEnhanced.icns"
chmod 0755 "${app}/Contents/MacOS/MeshCommander.Enhanced.Desktop" "${app}/Contents/MacOS/server/MeshCommander.Server"
codesign --force --sign - "${app}"
cp -a "${app}" "${staging}/"
ln -s /Applications "${staging}/Applications"
hdiutil create -volname "MeshCommander Enhanced" -srcfolder "${staging}" -ov -format UDZO \
  "${output_dir}/meshcommander-enhanced-macos-arm64.dmg"
