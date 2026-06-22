# 贡献指南

感谢您对 BreezeLink 项目的兴趣！我们欢迎各种形式的贡献，包括代码贡献、文档改进、问题报告等。

## 如何贡献

### 1. Fork 项目

首先，Fork 本项目到您的 GitHub 账户中：

```bash
git clone https://github.com/your-username/BreezeLink.git
cd BreezeLink
git remote add upstream https://github.com/BreezeLink/BreezeLink.git
```

### 2. 创建特性分支

为您的修改创建一个新的分支：

```bash
git checkout -b feature/amazing-feature
# 或
git checkout -b fix/bug-fix
```

### 3. 进行修改

进行您的代码修改，确保：

- 遵循现有的代码风格
- 添加适当的注释
- 更新相关文档
- 编写测试（如果适用）

### 4. 运行测试

确保您的修改不会破坏现有功能：

```bash
dotnet test
```

### 5. 提交更改

```bash
git add .
git commit -m "Add amazing feature"
```

### 6. 推送分支

```bash
git push origin feature/amazing-feature
```

### 7. 创建 Pull Request

在 GitHub 上创建 Pull Request，描述您的更改和改进。

## 开发规范

### 代码风格

- 使用 C# 8.0+ 特性
- 遵循 PascalCase 命名约定
- 使用有意义的变量和方法名
- 添加 XML 注释到公共 API

### 提交信息格式

使用以下格式的提交信息：

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

类型：
- `feat`: 新功能
- `fix`: 修复 bug
- `docs`: 文档更新
- `style`: 代码格式调整
- `refactor`: 重构
- `test`: 测试相关
- `chore`: 构建工具、辅助工具变动

### 分支命名

- `feature/`: 新功能分支
- `fix/`: 修复分支
- `docs/`: 文档分支
- `refactor/`: 重构分支

## 开发环境设置

请参考 [Development-Setup.md](docs/Development-Setup.md) 文档设置开发环境。

## 报告问题

如果您发现 bug 或有功能建议，请：

1. 搜索现有的 [Issues](https://github.com/BreezeLink/BreezeLink/issues)
2. 如果没有相关 issue，创建一个新的 issue
3. 提供详细的描述、复现步骤和环境信息

## 功能建议

我们欢迎功能建议！请：

1. 描述您想要的功能
2. 解释为什么这个功能有用
3. 提供使用场景示例

## 代码审查

所有提交的代码都需要经过代码审查。审查者会：

- 检查代码质量
- 验证功能正确性
- 确保遵循项目规范
- 评估性能影响

## 许可证

通过贡献代码，您同意您的贡献将根据项目的 MIT 许可证进行许可。

## 致谢

感谢所有为 BreezeLink 项目做出贡献的开发者！您的努力让项目变得更好。
