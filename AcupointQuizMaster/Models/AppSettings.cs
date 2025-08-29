using Newtonsoft.Json;

namespace AcupointQuizMaster.Models
{
    /// <summary>
    /// AI平台枚举
    /// </summary>
    public enum AIPlatform
    {
        DeepSeek,
        OpenAI,
        Gemini,
        Claude,
        Qwen,
        Baichuan,
        ChatGLM,
        Custom
    }

    /// <summary>
    /// AI平台配置信息
    /// </summary>
    public class PlatformConfig
    {
        public string Name { get; set; } = string.Empty;
        public string ApiUrl { get; set; } = string.Empty;
        public string DefaultModel { get; set; } = string.Empty;
        public string[] SupportedModels { get; set; } = Array.Empty<string>();
        public bool RequiresCustomUrl { get; set; } = false;

        public static Dictionary<AIPlatform, PlatformConfig> GetConfigs()
        {
            return new Dictionary<AIPlatform, PlatformConfig>
            {
                [AIPlatform.DeepSeek] = new PlatformConfig
                {
                    Name = "DeepSeek",
                    ApiUrl = "https://api.deepseek.com/v1/chat/completions",
                    DefaultModel = "deepseek-chat",
                    SupportedModels = new[] { "deepseek-chat", "deepseek-coder" }
                },
                [AIPlatform.OpenAI] = new PlatformConfig
                {
                    Name = "OpenAI",
                    ApiUrl = "https://api.openai.com/v1/chat/completions",
                    DefaultModel = "gpt-3.5-turbo",
                    SupportedModels = new[] { "gpt-3.5-turbo", "gpt-4", "gpt-4-turbo", "gpt-4o", "gpt-4o-mini" }
                },
                [AIPlatform.Gemini] = new PlatformConfig
                {
                    Name = "Google Gemini",
                    ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent",
                    DefaultModel = "gemini-1.5-flash",
                    SupportedModels = new[] { "gemini-1.5-flash", "gemini-1.5-pro", "gemini-pro" }
                },
                [AIPlatform.Claude] = new PlatformConfig
                {
                    Name = "Anthropic Claude",
                    ApiUrl = "https://api.anthropic.com/v1/messages",
                    DefaultModel = "claude-3-haiku-20240307",
                    SupportedModels = new[] { "claude-3-haiku-20240307", "claude-3-sonnet-20240229", "claude-3-opus-20240229" }
                },
                [AIPlatform.Qwen] = new PlatformConfig
                {
                    Name = "阿里通义千问",
                    ApiUrl = "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation",
                    DefaultModel = "qwen-turbo",
                    SupportedModels = new[] { "qwen-turbo", "qwen-plus", "qwen-max" }
                },
                [AIPlatform.Baichuan] = new PlatformConfig
                {
                    Name = "百川智能",
                    ApiUrl = "https://api.baichuan-ai.com/v1/chat/completions",
                    DefaultModel = "Baichuan2-Turbo",
                    SupportedModels = new[] { "Baichuan2-Turbo", "Baichuan2-53B" }
                },
                [AIPlatform.ChatGLM] = new PlatformConfig
                {
                    Name = "智谱ChatGLM",
                    ApiUrl = "https://open.bigmodel.cn/api/paas/v4/chat/completions",
                    DefaultModel = "glm-4",
                    SupportedModels = new[] { "glm-4", "glm-3-turbo" }
                },
                [AIPlatform.Custom] = new PlatformConfig
                {
                    Name = "自定义平台",
                    ApiUrl = "",
                    DefaultModel = "",
                    SupportedModels = new string[0],
                    RequiresCustomUrl = true
                }
            };
        }
    }

    /// <summary>
    /// 应用设置数据模型
    /// </summary>
    public class AppSettings
    {
        [JsonProperty("ai_platform")]
        public AIPlatform AiPlatform { get; set; } = AIPlatform.DeepSeek;

        [JsonProperty("api_url")]
        public string ApiUrl { get; set; } = string.Empty;

        [JsonProperty("api_key")]
        public string ApiKey { get; set; } = string.Empty;

        [JsonProperty("model_name")]
        public string ModelName { get; set; } = string.Empty;

        [JsonProperty("version")]
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// 创建默认设置
        /// </summary>
        /// <returns>默认设置实例</returns>
        public static AppSettings Default()
        {
            var defaultSettings = new AppSettings();
            var config = PlatformConfig.GetConfigs()[AIPlatform.DeepSeek];
            defaultSettings.ApiUrl = config.ApiUrl;
            defaultSettings.ModelName = config.DefaultModel;
            return defaultSettings;
        }

        /// <summary>
        /// 根据选择的平台更新URL和模型
        /// </summary>
        public void UpdateFromPlatform()
        {
            var configs = PlatformConfig.GetConfigs();
            if (configs.ContainsKey(AiPlatform))
            {
                var config = configs[AiPlatform];
                if (!config.RequiresCustomUrl)
                {
                    ApiUrl = config.ApiUrl;
                }
                if (string.IsNullOrEmpty(ModelName) || !config.SupportedModels.Contains(ModelName))
                {
                    ModelName = config.DefaultModel;
                }
            }
        }

        /// <summary>
        /// 验证设置是否有效
        /// </summary>
        /// <returns>是否有效</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(ApiUrl) && 
                   !string.IsNullOrWhiteSpace(ApiKey) && 
                   !string.IsNullOrWhiteSpace(ModelName);
        }

        /// <summary>
        /// 克隆设置对象
        /// </summary>
        /// <returns>克隆的设置对象</returns>
        public AppSettings Clone()
        {
            return new AppSettings
            {
                AiPlatform = this.AiPlatform,
                ApiUrl = this.ApiUrl,
                ApiKey = this.ApiKey,
                ModelName = this.ModelName,
                Version = this.Version
            };
        }
    }
}