# API配置说明

## 🔐 安全性说明

为了保护用户的API密钥安全，本应用不包含任何硬编码的API凭证。所有API配置需要用户在应用内手动设置。

## 🛠️ 配置AI功能

### 1. 获取API密钥
- **DeepSeek API（推荐）**：访问 https://platform.deepseek.com 注册账户并获取API密钥
- **OpenAI API**：访问 https://platform.openai.com 注册账户并获取API密钥
- **其他兼容OpenAI格式的API服务**

### 2. 在应用中配置
1. 打开应用
2. 点击"设置"按钮
3. 在"AI设置"区域填入：
   - **API地址**：例如 `https://api.deepseek.com/v1/chat/completions`
   - **API密钥**：您从服务商获取的密钥，格式如 `sk-xxxxxxxxxxxxxxxx`
   - **模型名称**：例如 `deepseek-chat`
4. 点击"测试连接"验证配置
5. 保存设置

### 3. 默认配置示例
- **DeepSeek**：
  - API地址：`https://api.deepseek.com/v1/chat/completions`
  - 模型：`deepseek-chat`
- **OpenAI**：
  - API地址：`https://api.openai.com/v1/chat/completions`
  - 模型：`gpt-3.5-turbo` 或 `gpt-4`

## 🔒 开发者配置

### 环境变量方式
如果您是开发者，可以通过环境变量设置默认API密钥：
```bash
export DEEPSEEK_API_KEY=your_api_key_here
```

### 本地配置文件（不会提交到版本控制）
您也可以创建本地配置文件，这些文件已在`.gitignore`中排除：
- `config.json`
- `appsettings.json`
- `local_settings.json`

## ⚠️ 重要安全提醒

1. **永远不要**在源代码中硬编码API密钥
2. **永远不要**将包含API密钥的文件提交到版本控制系统
3. 定期轮换API密钥
4. 监控API使用情况和费用
5. 为API密钥设置适当的权限和限制

## 📱 编译发布配置

### Android签名配置
发布APK时需要本地配置签名信息：

1. 创建密钥库文件：
```bash
keytool -genkey -v -keystore release-key.jks -alias app -keyalg RSA -keysize 2048 -validity 10000
```

2. 在项目文件中配置（本地）：
```xml
<AndroidKeyStore>true</AndroidKeyStore>
<AndroidSigningKeyStore>release-key.jks</AndroidSigningKeyStore>
<AndroidSigningKeyAlias>app</AndroidSigningKeyAlias>
<AndroidSigningKeyPass>您的密码</AndroidSigningKeyPass>
<AndroidSigningStorePass>您的密码</AndroidSigningStorePass>
```

或通过环境变量：
```bash
export ANDROID_KEYSTORE_PASS=your_password
export ANDROID_KEY_ALIAS=app
```

---

**注意**：此配置说明确保了应用的安全性，同时保持了功能的完整性。