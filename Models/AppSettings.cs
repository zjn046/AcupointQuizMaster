using Newtonsoft.Json;

namespace AcupointQuizMaster.Models
{
    /// <summary>
    /// 应用设置数据模型
    /// </summary>
    public class AppSettings
    {
        [JsonProperty("api_url")]
        public string ApiUrl { get; set; } = string.Empty;

        [JsonProperty("api_key")]
        public string ApiKey { get; set; } = string.Empty;

        [JsonProperty("model_name")]
        public string ModelName { get; set; } = string.Empty;

        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; } = 1000;

        [JsonProperty("temperature")]
        public float Temperature { get; set; } = 0.7f;

        [JsonProperty("top_p")]
        public float TopP { get; set; } = 0.9f;

        [JsonProperty("version")]
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// 创建默认设置
        /// </summary>
        /// <returns>默认设置实例</returns>
        public static AppSettings Default()
        {
            return DefaultApiConfig.GetDefaultSettings();
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
                ApiUrl = this.ApiUrl,
                ApiKey = this.ApiKey,
                ModelName = this.ModelName,
                MaxTokens = this.MaxTokens,
                Temperature = this.Temperature,
                TopP = this.TopP,
                Version = this.Version
            };
        }
    }
}