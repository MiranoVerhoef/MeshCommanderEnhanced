#!/usr/bin/env bash
set -euo pipefail

publish_dir="$(realpath "$1")"
output_dir="$(realpath -m "$2")"
version="${3:-0.2.0}"
root="${output_dir}/deb-root"
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/../.." && pwd)"

rm -rf "${root}"
mkdir -p "${root}/DEBIAN" "${root}/opt/meshcommander-enhanced" "${root}/usr/bin" \
  "${root}/usr/share/applications" "${root}/usr/share/icons/hicolor/256x256/apps"
cp -a "${publish_dir}/." "${root}/opt/meshcommander-enhanced/"
cp "${script_dir}/meshcommander-enhanced" "${root}/usr/bin/meshcommander-enhanced"
cp "${script_dir}/meshcommander-enhanced.desktop" "${root}/usr/share/applications/"
convert "${repo_root}/favicon.ico[0]" "${root}/usr/share/icons/hicolor/256x256/apps/meshcommander-enhanced.png"
sed "s/@VERSION@/${version}/g" "${script_dir}/control.in" > "${root}/DEBIAN/control"
chmod 0755 "${root}/usr/bin/meshcommander-enhanced" "${root}/opt/meshcommander-enhanced/MeshCommander.Enhanced.Desktop"
dpkg-deb --root-owner-group --build "${root}" "${output_dir}/meshcommander-enhanced-linux-x64.deb"
