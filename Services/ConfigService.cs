using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using AcupointQuizMaster.Models;

namespace AcupointQuizMaster.Services
{
    public class RemoteApiConfig
    {
        [JsonProperty("api_url")]
        public string ApiUrl { get; set; } = string.Empty;
        
        [JsonProperty("api_key")]
        public string ApiKey { get; set; } = string.Empty;
        
        [JsonProperty("model")]
        public string Model { get; set; } = string.Empty;
    }

    public class ApiPlatform
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ConfigUrl { get; set; } = string.Empty;
        public string[] AvailableModels { get; set; } = Array.Empty<string>();
    }

    public class ConfigService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        
        public static readonly ApiPlatform[] AvailablePlatforms = new[]
        {
            new ApiPlatform
            {
                Id = "openai",
                Name = "OpenAI",
                ConfigUrl = "",
                AvailableModels = new[] { "gpt-3.5-turbo", "gpt-4", "gpt-4o", "gpt-4o-mini" }
            },
            new ApiPlatform
            {
                Id = "claude",
                Name = "Claude (Anthropic)",
                ConfigUrl = "",
                AvailableModels = new[] { "claude-3-haiku-20240307", "claude-3-sonnet-20240229", "claude-3-opus-20240229" }
            },
            new ApiPlatform
            {
                Id = "deepseek",
                Name = "DeepSeek (欢喜就好提供)",
                ConfigUrl = "http://8.153.170.85:8000/config.json",
                AvailableModels = new[] { "deepseek-chat", "deepseek-coder" }
            }
        };

        public async Task<RemoteApiConfig?> GetRemoteConfigAsync(string configUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(configUrl))
                    return null;

                var response = await _httpClient.GetStringAsync(configUrl);
                var config = JsonConvert.DeserializeObject<RemoteApiConfig>(response);
                return config;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取远程配置失败: {ex.Message}");
                return null;
            }
        }

        public AppSettings CreateAppSettingsFromRemoteConfig(RemoteApiConfig remoteConfig, string selectedModel)
        {
            return new AppSettings
            {
                ApiUrl = remoteConfig.ApiUrl,
                ApiKey = remoteConfig.ApiKey,
                ModelName = selectedModel,
                MaxTokens = 1000,
                Temperature = 0.7f,
                TopP = 0.9f
            };
        }

        public ApiPlatform? GetPlatformById(string platformId)
        {
            foreach (var platform in AvailablePlatforms)
            {
                if (platform.Id == platformId)
                    return platform;
            }
            return null;
        }
    }
}