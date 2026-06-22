# BreezeLink

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![WinUI 3](https://img.shields.io/badge/WinUI-3.0-blue.svg)](https://learn.microsoft.com/en-us/windows/apps/winui/)


## ✨ 特性

- 🚀 **多协议支持**: 支持 SOCKS5、HTTP、Shadowsocks、VMess/VLESS、Trojan 等协议
- 🎨 **现代化界面**: 基于 WinUI 3 的美观用户界面，支持深浅色主题
- ⚡ **高性能**: 集成 sing-box 内核，提供卓越的代理性能

### 节点管理

- 📝 **节点配置**: 支持多种代理协议的节点配置
- 🔍 **延迟测试**: 实时测试节点延迟和可用性
- 📁 **分组管理**: 灵活的节点分组和组织
- 📊 **统计信息**: 节点状态统计和性能监控
- 📤 **导入导出**: 支持节点配置的导入和导出

### 高级功能

- 🔄 **配置重载**: 动态重载代理配置，无需重启
- 📈 **性能监控**: 实时监控代理性能指标
- 🛡️ **安全验证**: 支持 TLS 证书验证和跳过选项
- ⚙️ **灵活配置**: 支持多种加密方法和协议选项

## 📋 系统要求

- Windows 10 版本 1903 (19H1) 或更高
- Windows 11 (推荐)

## 🚀 快速开始

### 开发环境搭建

1. **安装 Visual Studio 2022**
   - 选择 **.NET 桌面开发** 和 **通用 Windows 平台开发** 工作负载
   - 安装 Windows App SDK

2. **克隆项目**
   ```bash
   git clone https://github.com/your-username/BreezeLink.git
   cd BreezeLink
   ```

3. **下载 sing-box**
   - 从 [sing-box 发布页](https://github.com/SagerNet/sing-box/releases) 下载最新版本的 Windows 版本
   - 将 `sing-box.exe` 放置到 `core-controller/sing-box/` 目录下

4. **编译运行**
   ```bash
   # 启动核心控制器服务
   cd core-controller
   dotnet run

   # 在新终端窗口启动 UI
   cd ../ui
   dotnet run
   ```

### 从安装包运行

1. 下载最新版本的 [BreezeLink 安装包](https://github.com/your-username/BreezeLink/releases)
2. 运行安装程序并按照提示完成安装
3. 从开始菜单启动 BreezeLink

## 📖 使用说明

### 基本操作

1. **启动代理**: 点击"启动代理"按钮启动 sing-box 内核
2. **停止代理**: 点击"停止代理"按钮停止代理服务
3. **查看状态**: "刷新状态"按钮可更新当前代理状态
4. **查看日志**: 实时查看代理运行日志

### 节点管理

- 在节点管理页面添加、编辑、删除代理节点
- 支持节点分组和自动切换
- 内置延迟测试功能

### 规则配置

- 自定义域名/IP/进程分流规则
- 支持导入/导出规则配置
- 实时规则生效

## 🏗️ 项目结构

```
BreezeLink/
├── core-controller/        # 后端控制服务
│   ├── src/               # 源代码
│   ├── sing-box/          # sing-box 内核
│   └── config.json        # 默认配置
├── ui/                    # WinUI 3 用户界面
│   ├── src/               # 源代码
│   ├── Assets/            # 应用资源
│   └── Package.appxmanifest # 应用清单
├── docs/                  # 文档
├── .github/workflows/     # CI/CD 工作流
├── README.md             # 项目说明
└── LICENSE               # MIT 许可证
```

## 🤝 贡献指南

我们欢迎任何形式的贡献！

### 开发环境

1. Fork 本项目
2. 创建特性分支: `git checkout -b feature/amazing-feature`
3. 提交更改: `git commit -m 'Add amazing feature'`
4. 推送分支: `git push origin feature/amazing-feature`
5. 创建 Pull Request

### 代码规范

- 遵循 C# 编码规范
- 使用有意义的变量和方法名
- 添加适当的注释
- 编写单元测试

## 📜 许可证

本项目采用 [MIT 许可证](LICENSE) 开源。

## 🙏 致谢

- [sing-box](https://github.com/SagerNet/sing-box) - 高性能代理内核
- [Microsoft WinUI](https://github.com/microsoft/microsoft-ui-xaml) - 现代化 UI 框架
- [CommunityToolkit](https://github.com/CommunityToolkit/Windows) - 优秀的开发工具包

## 📞 联系我们

- 📧 Email: support@breezelink.dev
- 💬 讨论: [GitHub Discussions](https://github.com/your-username/BreezeLink/discussions)
- 🐛 问题: [GitHub Issues](https://github.com/your-username/BreezeLink/issues)

---

**BreezeLink** - 让代理管理变得简单而强大
