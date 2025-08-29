using System;

namespace AcupointQuizMaster.Models
{
    /// <summary>
    /// 默认API配置类
    /// 包含默认的API密钥和配置，环境变量优先
    /// </summary>
    public static class DefaultApiConfig
    {
        // 默认DeepSeek API密钥 - 用户提供的key，环境变量优先
        public const string DEFAULT_DEEPSEEK_API_KEY = "sk-9d45aacc4d2e485eb5c98972c11beb1f";
        
        // 默认API地址
        public const string DEFAULT_API_URL = "https://api.deepseek.com/v1/chat/completions";
        
        // 默认模型名称
        public const string DEFAULT_MODEL = "deepseek-chat";
        
        /// <summary>
        /// 获取API密钥，环境变量优先
        /// </summary>
        /// <returns>API密钥</returns>
        public static string GetApiKey()
        {
            // 优先使用环境变量
            string? envApiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
            if (!string.IsNullOrEmpty(envApiKey))
            {
                return envApiKey;
            }
            
            // 如果环境变量不存在，使用默认key
            return DEFAULT_DEEPSEEK_API_KEY;
        }
        
        /// <summary>
        /// 获取API地址，环境变量优先
        /// </summary>
        /// <returns>API地址</returns>
        public static string GetApiUrl()
        {
            string? envApiUrl = Environment.GetEnvironmentVariable("DEEPSEEK_API_URL");
            if (!string.IsNullOrEmpty(envApiUrl))
            {
                return envApiUrl;
            }
            
            return DEFAULT_API_URL;
        }
        
        /// <summary>
        /// 获取模型名称，环境变量优先
        /// </summary>
        /// <returns>模型名称</returns>
        public static string GetModelName()
        {
            string? envModel = Environment.GetEnvironmentVariable("DEEPSEEK_MODEL");
            if (!string.IsNullOrEmpty(envModel))
            {
                return envModel;
            }
            
            return DEFAULT_MODEL;
        }
        
        /// <summary>
        /// 获取默认应用设置
        /// </summary>
        /// <returns>包含默认配置的AppSettings对象</returns>
        public static AppSettings GetDefaultSettings()
        {
            return new AppSettings
            {
                ApiUrl = GetApiUrl(),
                ApiKey = GetApiKey(),
                ModelName = GetModelName(),
                MaxTokens = 1000,
                Temperature = 0.7f,
                TopP = 0.9f
            };
        }
    }
}