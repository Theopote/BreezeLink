# BreezeLink 开发环境搭建指南

## 1. 系统要求

- Windows 10 版本 1903 (19H1) 或更高版本
- Windows 11 (推荐)
- 至少 4GB RAM
- 至少 2GB 可用磁盘空间

## 2. 安装 Visual Studio 2022

### 2.1 下载 Visual Studio 2022

从 [Visual Studio 官网](https://visualstudio.microsoft.com/downloads/) 下载 Visual Studio 2022 Community 版本（免费）。

### 2.2 安装工作负载

在安装过程中，选择以下工作负载：

1. **.NET 桌面开发**
   - .NET 8.0 运行时
   - .NET Framework 4.8 开发工具
   - C# 和 Visual Basic
   - .NET 桌面开发工具

2. **通用 Windows 平台开发**
   - C++ (v143) 通用 Windows 平台工具
   - MSVC v143 - VS 2022 C++ x64/x86 生成工具
   - Windows 10 SDK (10.0.22621.0)
   - Windows App SDK C# 模板

### 2.3 安装单个组件

确保安装以下组件：
- .NET 8.0 SDK
- NuGet 包管理器
- Git for Windows
- Windows App SDK

## 3. 安装 .NET 8.0 SDK

如果 Visual Studio 安装程序中没有包含 .NET 8.0 SDK，可以单独下载：

1. 访问 [.NET 8.0 下载页面](https://dotnet.microsoft.com/download/dotnet/8.0)
2. 下载 Windows x64 安装程序
3. 运行安装程序并按照提示完成安装

### 3.1 验证安装

打开命令提示符或 PowerShell，运行：

```bash
dotnet --version
```

应该显示 `8.0.xxx` 版本号。

## 4. 下载 sing-box 内核

### 4.1 下载 sing-box

1. 访问 [sing-box 发布页面](https://github.com/SagerNet/sing-box/releases)
2. 下载最新版本的 Windows 版本（通常是 `sing-box-windows-amd64.zip` 或类似名称）
3. 解压文件到 `core-controller/sing-box/` 目录下

### 4.2 验证 sing-box

```bash
cd core-controller/sing-box
./sing-box version
```

应该显示 sing-box 版本信息。

## 5. 克隆和构建项目

### 5.1 克隆项目

```bash
git clone https://github.com/your-username/BreezeLink.git
cd BreezeLink
```

### 5.2 恢复 NuGet 包

```bash
# 恢复所有项目的 NuGet 包
dotnet restore BreezeLink.sln
```

### 5.3 构建项目

```bash
# 构建调试版本
dotnet build BreezeLink.sln --configuration Debug

# 构建发布版本
dotnet build BreezeLink.sln --configuration Release
```

## 6. 运行项目

### 6.1 启动核心控制器

```bash
cd core-controller
dotnet run
```

核心控制器将在 `http://127.0.0.1:8800` 启动，提供以下 API 端点：
- `GET /start` - 启动代理
- `GET /stop` - 停止代理
- `GET /status` - 获取代理状态
- `GET /logs` - 获取运行日志

### 6.2 启动用户界面

在新终端窗口中：

```bash
cd ui
dotnet run
```

## 7. 调试和故障排除

### 7.1 常见问题

#### 问题 1: 找不到 sing-box.exe
**解决方案**: 确保 sing-box.exe 已正确放置在 `core-controller/sing-box/` 目录下，并且已从压缩包中解压。

#### 问题 2: 端口 8800 被占用
**解决方案**: 修改 `core-controller/src/Program.cs` 中的端口号，或关闭占用该端口的其他程序。

#### 问题 3: 构建失败，缺少 Windows App SDK
**解决方案**: 确保已安装 Windows App SDK 组件，并更新 Visual Studio。

### 7.2 调试技巧

1. **启用详细日志**: 在 `config.json` 中将日志级别改为 `debug`
2. **使用 Visual Studio 调试器**: 在 Visual Studio 中设置断点进行调试
3. **检查 Windows 事件查看器**: 查看应用程序和系统日志中的错误信息

## 8. 开发工具推荐

### 8.1 必需工具
- Visual Studio 2022
- .NET 8.0 SDK
- Git

### 8.2 可选工具
- [Windows Terminal](https://github.com/microsoft/terminal) - 更好的命令行体验
- [PowerShell 7](https://github.com/PowerShell/PowerShell) - 增强的命令行
- [Visual Studio Code](https://code.visualstudio.com/) - 轻量级代码编辑器
- [Fiddler](https://www.telerik.com/fiddler) - HTTP 调试工具

## 9. 下一步

完成环境搭建后，请：

1. 阅读项目 README.md 了解项目概况
2. 查看 `docs/` 目录下的详细文档
3. 运行示例代码验证环境配置
4. 开始实现你的第一个功能

如果遇到任何问题，请查看 GitHub Issues 或创建新的 Issue。
