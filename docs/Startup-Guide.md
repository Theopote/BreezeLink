# BreezeLink v0.1 启动和调试指南

## 🚀 快速启动

### 方法一：使用启动脚本（推荐）

1. **双击运行启动脚本**
   ```bash
   # Windows
   .\start.bat

   # 或者 PowerShell
   .\build.ps1
   ```

2. **脚本会自动执行以下操作：**
   - 启动核心控制器服务（端口 8800）
   - 启动 WinUI 3 用户界面
   - 显示 API 文档（Swagger UI）

### 方法二：手动启动

#### 1. 启动核心控制器服务

```bash
cd core-controller
dotnet run
```

**服务启动后会显示：**
- ✅ 核心控制器已启动
- 📡 API 地址: http://localhost:8800
- 📚 Swagger UI: http://localhost:8800

#### 2. 启动用户界面

在新终端窗口中：

```bash
cd ui
dotnet run
```

## 🔧 调试指南

### 开发环境设置

#### Visual Studio 2022 调试

1. **打开解决方案**
   ```bash
   BreezeLink.sln
   ```

2. **设置启动项目**
   - 右键解决方案 → "设置启动项目"
   - 选择 "多启动项目"
   - 设置两个项目：
     - `BreezeLink.CoreController`: "启动"
     - `BreezeLink.UI`: "启动"

3. **配置调试参数**
   - 右键 `BreezeLink.CoreController` → "属性"
   - 调试 → 启动选项 → "命令行参数": （留空）
   - 工作目录: `$(ProjectDir)..\`

#### Visual Studio Code 调试

创建 `.vscode/launch.json`:

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Launch Core Controller",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/core-controller/bin/Debug/net8.0/BreezeLink.CoreController.dll",
            "cwd": "${workspaceFolder}/core-controller",
            "console": "internalConsole",
            "internalConsoleOptions": "openOnSessionStart"
        },
        {
            "name": "Launch UI",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/ui/bin/Debug/net8.0-windows10.0.19041.0/BreezeLink.UI.dll",
            "cwd": "${workspaceFolder}/ui",
            "console": "internalConsole"
        }
    ],
    "compounds": [
        {
            "name": "Launch Both",
            "configurations": ["Launch Core Controller", "Launch UI"]
        }
    ]
}
```

### API 调试

#### 使用 Swagger UI

1. 启动核心控制器服务
2. 打开浏览器访问: http://localhost:8800
3. 使用 Swagger UI 测试 API 接口

#### 使用 curl 命令

```bash
# 健康检查
curl http://localhost:8800/api/proxy/health

# 获取状态
curl http://localhost:8800/api/proxy/status

# 启动代理
curl -X POST http://localhost:8800/api/proxy/start \
  -H "Content-Type: application/json" \
  -d '{"configContent": "{\"log\":{\"level\":\"info\"}}"}'

# 获取日志
curl http://localhost:8800/api/proxy/logs?lastLines=50

# 停止代理
curl -X POST http://localhost:8800/api/proxy/stop
```

#### 使用 Postman

1. 创建新的请求集合 "BreezeLink API"
2. 添加以下请求：
   - GET `http://localhost:8800/api/proxy/health`
   - GET `http://localhost:8800/api/proxy/status`
   - POST `http://localhost:8800/api/proxy/start`
   - POST `http://localhost:8800/api/proxy/stop`
   - GET `http://localhost:8800/api/proxy/logs`

## 📋 配置说明

### sing-box 内核配置

1. **下载 sing-box**
   - 从 [sing-box 发布页](https://github.com/SagerNet/sing-box/releases) 下载最新 Windows 版本
   - 解压到 `core-controller/sing-box/` 目录

2. **配置文件位置**
   - 默认配置: `core-controller/configs/config.json`
   - 运行时配置: `core-controller/bin/Debug/net8.0/configs/config.json`

3. **配置示例**
   ```json
   {
     "log": {
       "level": "info",
       "timestamp": true
     },
     "inbounds": [
       {
         "type": "mixed",
         "listen": "127.0.0.1",
         "listen_port": 1080
       }
     ],
     "outbounds": [
       {
         "type": "direct",
         "tag": "direct"
       }
     ],
     "route": {
       "final": "direct"
     }
   }
   ```

### 应用程序配置

#### 核心控制器配置

`core-controller/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ProxySettings": {
    "ConfigPath": "configs/config.json",
    "SingBoxPath": "sing-box/sing-box.exe",
    "ApiPort": 8800,
    "LogLevel": "info"
  }
}
```

#### 用户界面配置

`ui/src/appsettings.json`:

```json
{
  "AppSettings": {
    "Title": "BreezeLink",
    "Version": "0.1.0",
    "CoreControllerUrl": "http://127.0.0.1:8800",
    "AutoStartCoreController": false,
    "Theme": "System",
    "Language": "zh-CN"
  },
  "ProxySettings": {
    "DefaultPort": 1080,
    "HttpPort": 8080,
    "SocksPort": 1081
  }
}
```

## 🐛 故障排除

### 常见问题

#### 1. 核心控制器无法启动

**错误**: `sing-box executable not found`

**解决方案**:
1. 确保 sing-box.exe 位于 `core-controller/sing-box/` 目录
2. 检查文件权限
3. 重新下载 sing-box 并解压

#### 2. 端口 8800 被占用

**解决方案**:
1. 关闭占用端口的程序
2. 修改 `core-controller/src/Program.cs` 中的端口号
3. 重启计算机

#### 3. UI 无法连接到核心控制器

**错误**: `Connection error`

**解决方案**:
1. 确保核心控制器正在运行
2. 检查防火墙设置
3. 验证端口 8800 可访问

#### 4. 编译错误

**错误**: `Package not found`

**解决方案**:
1. 恢复 NuGet 包: `dotnet restore BreezeLink.sln`
2. 清理并重新构建: `dotnet clean && dotnet build`
3. 更新 Visual Studio

### 调试技巧

#### 启用详细日志

在 `appsettings.json` 中设置:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "BreezeLink": "Trace"
    }
  }
}
```

#### 使用调试器

1. **设置断点**
   - 在 `SingBoxProcessManager.StartAsync()` 中设置断点
   - 在 API 控制器方法中设置断点

2. **查看变量**
   - 检查进程状态
   - 查看日志内容
   - 验证配置加载

3. **性能分析**
   - 使用 Visual Studio 性能分析器
   - 监控内存使用
   - 检查网络请求

## 📊 日志和监控

### 日志文件位置

- **核心控制器日志**: `core-controller/logs/BreezeLink-yyyy-MM-dd.log`
- **应用程序日志**: 控制台输出

### 日志级别

- **Debug**: 详细调试信息
- **Information**: 一般信息
- **Warning**: 警告信息
- **Error**: 错误信息
- **Critical**: 严重错误

### 监控命令

```bash
# 检查进程状态
tasklist | findstr sing-box

# 检查端口占用
netstat -ano | findstr :8800

# 查看日志
tail -f core-controller/logs/BreezeLink-$(date +%Y-%m-%d).log
```

## 🔄 开发工作流

### 1. 代码修改

```bash
# 修改代码后
cd core-controller
dotnet build

cd ../ui
dotnet build
```

### 2. 热重载调试

```bash
# 核心控制器（支持热重载）
cd core-controller
dotnet watch run

# UI（WinUI 3 不支持热重载，需要重启）
cd ../ui
dotnet run
```

### 3. 发布构建

```bash
# 发布版本构建
dotnet publish BreezeLink.sln --configuration Release --runtime win-x64

# 输出位置
# core-controller/bin/Release/net8.0/win-x64/publish/
# ui/bin/Release/net8.0-windows10.0.19041.0/win-x64/BreezeLink.UI/
```

## 📞 获取帮助

- 📖 **完整文档**: 查看 `docs/` 目录
- 🐛 **问题反馈**: [GitHub Issues](https://github.com/your-username/BreezeLink/issues)
- 💬 **功能建议**: [GitHub Discussions](https://github.com/your-username/BreezeLink/discussions)

---

**🎉 享受 BreezeLink 开发之旅！**
