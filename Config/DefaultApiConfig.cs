using System;

namespace AcupointQuizMaster.Models
{
    /// <summary>
    /// 默认API配置类
    /// 提供基本的默认配置，不包含敏感信息
    /// </summary>
    public static class DefaultApiConfig
    {
        /// <summary>
        /// 获取默认应用设置
        /// </summary>
        /// <returns>包含默认配置的AppSettings对象</returns>
        public static AppSettings GetDefaultSettings()
        {
            return new AppSettings
            {
                ApiPlatform = "deepseek",
                ApiUrl = "",
                ApiKey = "",
                ModelName = "deepseek-chat",
                MaxTokens = 1000,
                Temperature = 0.7f,
                TopP = 0.9f
            };
        }
    }
}