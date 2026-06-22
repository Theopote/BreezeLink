# 🚀 BreezeLink 完整运行指南

## 📋 环境准备

### 1. 系统要求
- Windows 10 版本 1903 (19H1) 或更高
- Windows 11 (推荐)
- .NET 8.0 运行时
- 至少 4GB RAM
- 至少 2GB 可用磁盘空间

### 2. 验证环境

#### 检查 .NET 8.0
```bash
dotnet --version
```
应该显示 `8.0.xxx` 版本号。

#### 检查 sing-box
1. 下载 sing-box 从 [sing-box 发布页](https://github.com/SagerNet/sing-box/releases)
2. 解压到 `core-controller/sing-box/` 目录
3. 验证: `cd core-controller/sing-box && ./sing-box version`

## 🎯 快速启动

### 方法一：使用启动脚本（推荐）

```bash
# 1. 启动核心控制器服务
cd core-controller
dotnet run

# 2. 启动用户界面（新终端窗口）
cd ../ui
dotnet run
```

### 方法二：使用批处理脚本

```bash
# 双击运行
.\start.bat
```

### 方法三：使用 PowerShell 脚本

```bash
.\build.ps1
```

## 🔧 详细启动步骤

### 步骤 1：启动核心控制器服务

```bash
cd BreezeLink/core-controller
dotnet run
```

**启动成功后会看到：**
```
🚀 Starting BreezeLink Core Controller v0.3...
📡 API will be available at: http://localhost:8800
📚 Swagger UI: http://localhost:8800
✨ New features: Node Management, Rule Management, Traffic Monitoring, Auto Switch, Config Import
Press Ctrl+C to stop the service
```

### 步骤 2：启动用户界面

在新终端窗口中：

```bash
cd BreezeLink/ui
dotnet run
```

**启动成功后会看到：**
```
BreezeLink UI started successfully
```

### 步骤 3：验证服务状态

#### 检查 API 可用性
```bash
curl http://localhost:8800/api/proxy/health
```

应该返回：
```json
{
  "success": true,
  "message": "Service is healthy",
  "data": null
}
```

#### 访问 Swagger UI
打开浏览器访问：http://localhost:8800

## 🎨 使用 BreezeLink

### 1. 主界面功能

#### 代理控制
- **启动代理**: 点击"启动代理"按钮启动 sing-box 内核
- **停止代理**: 点击"停止代理"按钮停止代理服务
- **重载配置**: 点击"重载配置"动态更新代理配置
- **状态监控**: 查看实时代理状态和进程信息

#### 节点管理
- **节点管理**: 点击"节点管理"按钮打开节点管理界面
- **配置导入**: 支持导入 Clash、V2Ray、sing-box 等格式配置
- **延迟测试**: 测试节点延迟和可用性
- **节点分组**: 灵活的节点分组管理

#### 快速操作
- **打开配置**: 直接编辑 sing-box 配置文件
- **测试连接**: 验证代理服务连接状态

### 2. 节点管理界面

#### 统计面板
- 查看总节点数、活跃节点数、分组数
- 实时显示测试成功率和平均延迟

#### 节点操作
1. **创建分组**: 点击"创建分组"按钮
2. **添加节点**: 选择分组，填写节点信息
3. **测试节点**: 选中节点，点击"测试选中节点"
4. **批量测试**: 点击"批量测试"测试所有活跃节点

#### 支持的协议
- Shadowsocks (SS)
- VMess
- VLESS
- Trojan
- SOCKS5
- HTTP

### 3. API 测试

#### 使用 Swagger UI
1. 打开 http://localhost:8800
2. 展开各个 API 端点
3. 点击"Try it out"进行测试

#### 常用 API 测试命令

```bash
# 健康检查
curl http://localhost:8800/api/proxy/health

# 获取代理状态
curl http://localhost:8800/api/proxy/status

# 启动代理
curl -X POST http://localhost:8800/api/proxy/start \
  -H "Content-Type: application/json" \
  -d '{"configContent": "{\"log\":{\"level\":\"info\"}}"}'

# 获取节点列表
curl http://localhost:8800/api/nodes

# 测试节点
curl -X POST http://localhost:8800/api/nodes/{nodeId}/test

# 获取流量统计
curl http://localhost:8800/api/traffic/stats
```

## 🔍 功能验证

### 1. 代理功能测试

1. **启动代理服务**
   - 在主界面点击"启动代理"
   - 等待状态变为"运行中"

2. **配置系统代理**
   - Windows 设置 → 网络和 Internet → 代理
   - 手动代理设置: 127.0.0.1:1080 (SOCKS5)
   - 或 127.0.0.1:8080 (HTTP)

3. **验证代理**
   - 访问 http://ipinfo.io 检查 IP 地址
   - 应该显示代理服务器的 IP

### 2. 节点管理测试

1. **导入节点配置**
   ```bash
   curl -X POST http://localhost:8800/api/config/import \
     -H "Content-Type: application/json" \
     -d '{
       "configContent": "你的代理配置内容",
       "configFormat": "clash"
     }'
   ```

2. **测试节点延迟**
   ```bash
   curl -X POST http://localhost:8800/api/nodes/{nodeId}/test
   ```

3. **查看节点统计**
   ```bash
   curl http://localhost:8800/api/nodes/statistics
   ```

### 3. 流量监控测试

1. **启动流量监控**
   - 代理运行时自动启动流量监控
   - 查看实时流量统计

2. **查看流量数据**
   ```bash
   curl http://localhost:8800/api/traffic/stats
   ```

## 🛠️ 故障排除

### 常见问题和解决方案

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

#### 5. 代理无法连接

**错误**: `Connection failed`

**解决方案**:
1. 检查节点配置是否正确
2. 验证服务器地址和端口
3. 检查网络连接
4. 测试节点延迟

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

#### 查看日志文件

- 核心控制器日志: `core-controller/logs/BreezeLink-yyyy-MM-dd.log`
- 应用程序日志: 控制台输出

#### 性能监控

```bash
# 检查进程状态
tasklist | findstr sing-box

# 检查端口占用
netstat -ano | findstr :8800

# 查看系统资源
taskmgr
```

## 📊 监控和维护

### 1. 实时监控

#### 系统托盘
- 查看代理运行状态
- 快速切换节点
- 接收重要通知

#### 流量监控
- 实时流量统计
- 连接数监控
- 速度显示

### 2. 日志管理

#### 日志轮转
- 自动日志轮转（10MB）
- 保留最近5个日志文件
- 按日期命名

#### 日志级别
- **Debug**: 详细调试信息
- **Information**: 一般信息
- **Warning**: 警告信息
- **Error**: 错误信息
- **Critical**: 严重错误

### 3. 数据备份

#### 自动备份
- 节点配置自动保存
- 规则配置自动保存
- 应用程序设置自动保存

#### 手动备份
```bash
# 备份数据目录
xcopy /E /I core-controller\data backup\data

# 备份配置文件
xcopy /E /I core-controller\configs backup\configs
```

## 🔄 高级配置

### 1. 自定义配置

#### 修改监听端口
编辑 `core-controller/src/Program.cs`:
```csharp
webBuilder.UseKestrel(options =>
{
    options.ListenAnyIP(8800); // 修改为其他端口
});
```

#### 修改日志级别
编辑 `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "BreezeLink": "Debug"
    }
  }
}
```

### 2. 性能调优

#### 连接池设置
```csharp
services.AddHttpClient("proxy", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.MaxResponseContentBufferSize = 1024 * 1024; // 1MB
});
```

#### 内存优化
- 定期清理旧的统计数据
- 限制日志文件大小
- 优化数据结构

## 📞 获取帮助

### 技术支持
- 📖 **文档**: 查看 `docs/` 目录
- 🐛 **问题反馈**: [GitHub Issues](https://github.com/your-username/BreezeLink/issues)
- 💬 **功能建议**: [GitHub Discussions](https://github.com/your-username/BreezeLink/discussions)

### 社区支持
- 📧 Email: support@breezelink.dev
- 💬 讨论: [GitHub Discussions](https://github.com/your-username/BreezeLink/discussions)
- 🐛 问题: [GitHub Issues](https://github.com/your-username/BreezeLink/issues)

---

## 🎉 享受 BreezeLink！

现在您已经掌握了 BreezeLink 的完整使用方法。BreezeLink 提供了：

- ✅ **完整的代理管理**: 多协议支持，灵活配置
- ✅ **智能节点管理**: 自动测试，延迟优化
- ✅ **实时监控**: 流量统计，性能监控
- ✅ **用户友好**: 现代化界面，简单易用
- ✅ **高度可扩展**: 插件架构，持续更新

**开始您的代理管理之旅吧！** 🚀✨
