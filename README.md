# 穴位测验大师 🎯

一个专业的中医针灸学习应用，支持穴位抽取练习和AI智能测评功能。

## 📱 功能特色

### 🎲 穴位抽取练习
- 支持16个经络题库的随机抽取
- 包含手太阴肺经、手阳明大肠经、足阳明胃经等完整经络系统
- 支持盲医考必背穴位专项练习
- 实时统计答题正确率

### 🤖 AI智能测评
- 支持多个主流AI平台
  - DeepSeek（深度求索）
  - OpenAI（GPT系列）
  - Google Gemini
  - Anthropic Claude
  - 阿里通义千问
  - 百川智能
  - 智谱ChatGLM
  - 自定义平台
- 智能出题：定位、主治、归经等多维度测试
- 智能判卷：多维度评分系统，准确评估答题质量
- 详细反馈：提供改进建议和标准答案对比

### ⚙️ 便捷设置
- 一键选择AI平台，自动配置API地址
- 支持自定义API配置
- 连接测试功能确保设置正确

## 📥 下载安装

### 最新版本：v1.1

**多架构版本（推荐）：**
- [ARM64版本](https://github.com/zjn046/AcupointProject/releases/download/v1.1/com.acupoint.quizmaster-v1.1-arm64.apk) - 适用于大多数现代安卓设备（约15MB）
- [ARM版本](https://github.com/zjn046/AcupointProject/releases/download/v1.1/com.acupoint.quizmaster-v1.1-arm.apk) - 适用于较老的安卓设备（约14MB）  
- [x64版本](https://github.com/zjn046/AcupointProject/releases/download/v1.1/com.acupoint.quizmaster-v1.1-x64.apk) - 适用于x86_64架构设备（约16MB）
- [x86版本](https://github.com/zjn046/AcupointProject/releases/download/v1.1/com.acupoint.quizmaster-v1.1-x86.apk) - 适用于x86架构设备（约15MB）

**通用版本：**
- [通用APK](https://github.com/zjn046/AcupointProject/releases/download/v1.1/com.acupoint.quizmaster-v1.1-universal.apk) - 兼容所有架构（约45MB）

> 💡 **选择建议**：优先下载对应架构的版本，文件更小，运行更高效。不确定设备架构的用户可选择通用版本。

## 🔄 更新日志

### Version 1.1 (2024-12-29)
**🚀 新功能**
- ✨ 新增多平台AI支持：DeepSeek、OpenAI、Gemini、Claude、通义千问、百川智能、ChatGLM、自定义平台
- 🎯 设置页面增加AI平台选择下拉框，支持一键切换
- 🔄 智能填充各平台API地址和默认模型配置
- 🛠️ 重构AI服务架构，统一不同平台的调用接口

**🎨 界面改进**
- 📱 抽穴位显示改为只读模式，避免误操作
- ✨ 优化设置界面布局和用户体验
- 🔧 改进错误提示和状态反馈

**🏗️ 技术优化**
- 🚀 支持分架构编译，大幅减小APK体积
- 🔧 统一不同AI平台的响应格式处理
- 🛡️ 增强错误处理和兜底机制

### Version 1.0
- 🎉 首个正式版本发布
- 🎲 穴位抽取练习功能
- 🤖 基础AI测评功能（DeepSeek）
- 📊 练习统计和结果查看

## 🛠️ 技术栈

- **开发框架**：.NET 9 for Android
- **AI集成**：支持多平台API调用
- **数据存储**：本地JSON配置
- **网络请求**：HttpClient
- **JSON处理**：Newtonsoft.Json

## 🤝 贡献

欢迎提交Issue和Pull Request来帮助改进这个项目！

## 📄 许可证

本项目采用MIT许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 📧 联系方式

如有问题或建议，请通过GitHub Issues联系我们。

---

⭐ 如果这个项目对您有帮助，请给我们一个星标！
