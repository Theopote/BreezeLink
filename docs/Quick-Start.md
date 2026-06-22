# BreezeLink 快速开始指南

## 🎉 欢迎使用 BreezeLink！

本指南将帮助您快速启动和运行 BreezeLink 代理客户端。

## 📋 环境准备

### 1. 系统要求
- Windows 10 1903+ 或 Windows 11
- .NET 8.0 运行时
- 管理员权限（用于安装代理内核）

### 2. 快速安装

#### 方法一：使用安装包（推荐）
1. 从 [GitHub Releases](https://github.com/your-username/BreezeLink/releases) 下载最新版本
2. 运行 `BreezeLink-Setup.exe`
3. 按照安装向导完成安装
4. 从开始菜单启动 BreezeLink

#### 方法二：从源码运行
```bash
# 克隆项目
git clone https://github.com/your-username/BreezeLink.git
cd BreezeLink

# 启动核心控制器
cd core-controller
dotnet run

# 在新终端启动 UI
cd ../ui
dotnet run
```

## 🚀 基本使用

### 1. 启动代理服务
1. 打开 BreezeLink 应用程序
2. 点击 **"启动代理"** 按钮
3. 等待状态变为 "Running"

### 2. 配置代理
1. 打开系统代理设置
2. 配置为 `127.0.0.1:1080` (SOCKS5)
3. 或 `127.0.0.1:8080` (HTTP)

### 3. 验证代理
- 访问 [ipinfo.io](https://ipinfo.io) 检查 IP 地址
- 确保显示的 IP 不是您的真实 IP

## ⚙️ 高级配置

### 节点管理
1. 点击 **"节点管理"** 按钮
2. 添加您的代理服务器信息
3. 支持多种协议：Shadowsocks、VMess、VLESS 等

### 规则配置
1. 点击 **"规则配置"** 按钮
2. 添加域名/IP分流规则
3. 设置直连/代理策略

### 延迟测试
1. 在节点列表中选择节点
2. 点击 **"测试延迟"** 按钮
3. 查看连接质量

## 🔍 故障排除

### 常见问题

#### Q: 无法启动代理服务
**A**: 检查是否已下载 sing-box 内核并放置在正确位置

#### Q: 代理无法连接
**A**: 检查代理服务器配置是否正确，网络连接是否正常

#### Q: 应用程序崩溃
**A**: 检查 .NET 8.0 运行时是否正确安装

### 获取帮助
- 📖 阅读完整文档: [Development-Setup.md](docs/Development-Setup.md)
- 🐛 报告问题: [GitHub Issues](https://github.com/your-username/BreezeLink/issues)
- 💬 讨论交流: [GitHub Discussions](https://github.com/your-username/BreezeLink/discussions)

## 📚 更多资源

- 🏠 [项目主页](https://github.com/your-username/BreezeLink)
- 📖 [完整文档](docs/)
- 🤝 [贡献指南](CONTRIBUTING.md)
- 🗺️ [开发路线图](ROADMAP.md)

---

**享受 BreezeLink 带来的便捷代理体验！** 🎊
