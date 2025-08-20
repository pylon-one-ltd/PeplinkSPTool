#!/bin/bash
sign_cert="8E16D2DA79ABB27BEF3812329ACF9F40A883CA08"
credential_profile="AppleDev-Bottswana55-PylonOne"

rm -rf release-artifacts > /dev/null 2>&1
mkdir release-artifacts

cd PeplinkSPTool

echo -- Windows Build --
rm -rf windows-build > /dev/null 2>&1
mkdir windows-build

echo Build arm64
dotnet publish -c Release -r win-arm64

echo Build amd64
dotnet publish -c Release -r win-amd64

cp bin/Release/net9.0/win-x64/publish/PeplinkSPTool.exe windows-build/PeplinkSPTool-amd64.exe
cp bin/Release/net9.0/win-arm64/publish/PeplinkSPTool.exe windows-build/PeplinkSPTool-arm64.exe
zip -j ../release-artifacts/PeplinkSPTool-win-amd64.zip windows-build/PeplinkSPTool-amd64.exe appsettings.json
zip -j ../release-artifacts/PeplinkSPTool-win-arm64.zip windows-build/PeplinkSPTool-arm64.exe appsettings.json

echo -- MacOS Build --

rm -rf macos-build > /dev/null 2>&1
mkdir macos-build

echo Build arm64
dotnet publish -c Release -r osx-arm64

echo Build amd64
dotnet publish -c Release -r osx-amd64

echo Build universal binary
lipo -create -output macos-build/PeplinkSPTool bin/Release/net9.0/osx-arm64/publish/PeplinkSPTool bin/Release/net9.0/osx-x64/publish/PeplinkSPTool
chmod +x macos-build/PeplinkSPTool

echo Sign Binary
codesign --force --verbose --timestamp --sign $sign_cert --options=runtime --entitlements ../entitlements.plist macos-build/PeplinkSPTool

echo Notorise Binary
ditto -c --sequesterRsrc -k -V macos-build/PeplinkSPTool macos-build/PeplinkSPTool.zip
xcrun notarytool submit macos-build/PeplinkSPTool.zip --wait --keychain-profile $credential_profile
zip -j ../release-artifacts/PeplinkSPTool-mac-universal.zip macos-build/PeplinkSPTool appsettings.json