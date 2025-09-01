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
                Id = "deepseek",
                Name = "DeepSeek (欢喜就好提供)",
                ConfigUrl = "http://8.153.170.85:8000/config.json",
                AvailableModels = new[] { "deepseek-chat", "deepseek-coder" }
            },
            new ApiPlatform
            {
                Id = "openai",
                Name = "OpenAI",
                ConfigUrl = "",
                AvailableModels = new[] { "gpt-3.5-turbo", "gpt-4", "gpt-4o", "gpt-4o-mini", "gpt-4-turbo" }
            },
            new ApiPlatform
            {
                Id = "claude",
                Name = "Claude (Anthropic)",
                ConfigUrl = "",
                AvailableModels = new[] { "claude-3-haiku-20240307", "claude-3-sonnet-20240229", "claude-3-opus-20240229", "claude-3-5-sonnet-20241022" }
            },
            new ApiPlatform
            {
                Id = "gemini",
                Name = "Google Gemini",
                ConfigUrl = "",
                AvailableModels = new[] { "gemini-pro", "gemini-pro-vision", "gemini-1.5-pro", "gemini-1.5-flash" }
            },
            new ApiPlatform
            {
                Id = "moonshot",
                Name = "月之暗面 Kimi",
                ConfigUrl = "",
                AvailableModels = new[] { "moonshot-v1-8k", "moonshot-v1-32k", "moonshot-v1-128k" }
            },
            new ApiPlatform
            {
                Id = "qianwen",
                Name = "阿里云通义千问",
                ConfigUrl = "",
                AvailableModels = new[] { "qwen-plus", "qwen-turbo", "qwen-max", "qwen-max-longcontext" }
            },
            new ApiPlatform
            {
                Id = "baidu",
                Name = "百度文心一言",
                ConfigUrl = "",
                AvailableModels = new[] { "ernie-4.0-8k", "ernie-3.5-8k", "ernie-turbo-8k", "ernie-speed-128k" }
            },
            new ApiPlatform
            {
                Id = "zhipu",
                Name = "智谱AI GLM",
                ConfigUrl = "",
                AvailableModels = new[] { "glm-4", "glm-4v", "glm-3-turbo" }
            },
            new ApiPlatform
            {
                Id = "custom",
                Name = "第三方平台",
                ConfigUrl = "",
                AvailableModels = new[] { "custom-model-1", "custom-model-2" }
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