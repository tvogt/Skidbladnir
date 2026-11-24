#!/bin/zsh
# Builds Skidbladnir.dll and writes dist/Lemuria-Skidbladnir-<version>.zip
# (Thunderstore layout: manifest.json, README.md, CHANGELOG.md, icon.png, plugins/Skidbladnir.dll).
# Game references: see README "Building from source" (RefsDir).
set -e
cd "${0:A:h}"
# Local toolchain of the parent workspace, if present (keeps dotnet caches out of $HOME).
[[ -f ../.tools/env.sh ]] && source ../.tools/env.sh
dotnet build -c Release "$@"

VERSION=$(sed -n 's/.*"version_number": "\(.*\)".*/\1/p' package/manifest.json)
grep -q "Version = \"$VERSION\"" Plugin.cs || { echo "manifest version $VERSION != Plugin.Version"; exit 1; }

STAGE=dist/stage
rm -rf "$STAGE"; mkdir -p "$STAGE/plugins"
cp package/{manifest.json,icon.png} README.md CHANGELOG.md "$STAGE/"
cp bin/Release/Skidbladnir.dll "$STAGE/plugins/"
ZIP="$PWD/dist/Lemuria-Skidbladnir-$VERSION.zip"
rm -f "$ZIP"
(cd "$STAGE" && zip -qr -X "$ZIP" .)
rm -rf "$STAGE"
chmod -R a+rwX . 2>/dev/null || true
unzip -l "$ZIP"
