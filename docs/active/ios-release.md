# 星轨塔罗 iOS / Mac Catalyst 发布指南

> 适用版本：IChing.Tarot.App v1.1+  
> 前提：macOS + Xcode 15+ + Apple Developer 账号

## 1. 证书与描述文件

1. 在 [Apple Developer](https://developer.apple.com/account) 创建 App ID：`com.iching.tarot`
2. 创建 **Apple Distribution** 证书（App Store）或 **Development** 证书（TestFlight 内测）
3. 创建 Provisioning Profile，绑定 `com.iching.tarot`

## 2. 本地打包

```bash
cd src/IChing.Tarot.App

# iOS 设备包（Release）
dotnet publish -f net10.0-ios -c Release \
  -p:ArchiveOnBuild=true \
  -p:RuntimeIdentifier=ios-arm64 \
  -p:CodesignKey="Apple Distribution: YOUR TEAM" \
  -p:CodesignProvision="IChing Tarot AppStore"

# Mac Catalyst（可选，Mac App Store）
dotnet publish -f net10.0-maccatalyst -c Release \
  -p:RuntimeIdentifier=maccatalyst-x64 \
  -p:CodesignKey="Apple Distribution: YOUR TEAM"
```

Windows 上无法完成 iOS 签名；请在 macOS 或 CI macOS runner 上执行。

## 3. 脚本入口

```bash
# macOS / Linux
bash scripts/publish-tarot-app-ios.sh

# 环境变量（可选）
export IOS_CODESIGN_KEY="Apple Distribution: ..."
export IOS_PROVISION_PROFILE="IChing Tarot AppStore"
```

## 4. TestFlight / App Store

1. 用 Xcode **Organizer** 或 `xcrun altool` 上传 `.ipa`
2. App Store Connect 填写截图、隐私政策、年龄分级
3. 塔罗类应用建议说明：内容为娱乐/自我反思，非医疗或投资建议

## 5. 常见问题

| 问题 | 处理 |
|------|------|
| 路径含中文导致 Android 构建失败 | 使用 `scripts/ensure-android-ascii-path.cmd` 的 junction |
| iOS 网络访问 Lab API | Info.plist 需允许本地网络 / ATS 例外（仅 Debug） |
| 版本号 | 修改 `IChing.Tarot.App.csproj` 中 `ApplicationDisplayVersion` / `ApplicationVersion` |

## 6. 与 Android / Windows 对齐

| 平台 | 脚本 | 产物 |
|------|------|------|
| Windows | `scripts/publish-tarot-app-v1.1.cmd` | zip |
| Android | 同上 | apk |
| iOS | `scripts/publish-tarot-app-ios.sh` | ipa（需 macOS） |
