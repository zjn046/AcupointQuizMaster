using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Android.Content;
using Newtonsoft.Json;
using AcupointQuizMaster.Models;

namespace AcupointQuizMaster.Services
{
    /// <summary>
    /// 持久化管理服务 - 对应Python中的PersistenceManager类
    /// </summary>
    public class PersistenceService
    {
        private readonly Context _context;
        private readonly string _configFileName = "config.json";
        private readonly string _settingsFileName = "settings.json";

        public PersistenceService(Context context)
        {
            _context = context;
        }

        /// <summary>
        /// 加载已使用的穴位记录
        /// </summary>
        /// <returns>题库名到已使用穴位集合的映射</returns>
        public Dictionary<string, HashSet<string>> LoadUsedItems()
        {
            try
            {
                var configPath = Path.Combine(_context.FilesDir?.AbsolutePath ?? "", _configFileName);
                
                if (!File.Exists(configPath))
                {
                    return new Dictionary<string, HashSet<string>>();
                }

                var jsonContent = File.ReadAllText(configPath, Encoding.UTF8);
                var config = JsonConvert.DeserializeObject<ConfigData>(jsonContent);
                
                var result = new Dictionary<string, HashSet<string>>();
                if (config?.UsedItems != null)
                {
                    foreach (var kvp in config.UsedItems)
                    {
                        result[kvp.Key] = new HashSet<string>(kvp.Value);
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                // 如果加载失败，返回空字典
                System.Diagnostics.Debug.WriteLine($"加载配置失败: {ex.Message}");
                return new Dictionary<string, HashSet<string>>();
            }
        }

        /// <summary>
        /// 保存已使用的穴位记录
        /// </summary>
        /// <param name="usedItemsMap">题库名到已使用穴位集合的映射</param>
        /// <returns>是否保存成功</returns>
        public bool SaveUsedItems(Dictionary<string, HashSet<string>> usedItemsMap)
        {
            try
            {
                var configPath = Path.Combine(_context.FilesDir?.AbsolutePath ?? "", _configFileName);
                
                var config = new ConfigData
                {
                    Version = "1.0",
                    UsedItems = new Dictionary<string, List<string>>()
                };

                foreach (var kvp in usedItemsMap)
                {
                    config.UsedItems[kvp.Key] = new List<string>(kvp.Value);
                }

                var jsonContent = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configPath, jsonContent, Encoding.UTF8);
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存配置失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 清空指定题库的已使用记录
        /// </summary>
        /// <param name="bankName">题库名称</param>
        /// <returns>是否清空成功</returns>
        public bool ClearUsedItems(string bankName)
        {
            try
            {
                var usedItemsMap = LoadUsedItems();
                
                if (usedItemsMap.ContainsKey(bankName))
                {
                    usedItemsMap[bankName].Clear();
                    return SaveUsedItems(usedItemsMap);
                }
                
                return true; // 如果没有记录，视为成功
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清空记录失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取指定题库的已使用穴位数量
        /// </summary>
        /// <param name="bankName">题库名称</param>
        /// <returns>已使用的穴位数量</returns>
        public int GetUsedItemsCount(string bankName)
        {
            try
            {
                var usedItemsMap = LoadUsedItems();
                return usedItemsMap.TryGetValue(bankName, out var usedSet) ? usedSet.Count : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 检查指定穴位是否已被使用
        /// </summary>
        /// <param name="bankName">题库名称</param>
        /// <param name="acupointName">穴位名称</param>
        /// <returns>是否已被使用</returns>
        public bool IsItemUsed(string bankName, string acupointName)
        {
            try
            {
                var usedItemsMap = LoadUsedItems();
                return usedItemsMap.TryGetValue(bankName, out var usedSet) && usedSet.Contains(acupointName);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 加载应用设置
        /// </summary>
        /// <returns>应用设置对象</returns>
        public AppSettings LoadSettings()
        {
            try
            {
                var settingsPath = Path.Combine(_context.FilesDir?.AbsolutePath ?? "", _settingsFileName);
                
                if (!File.Exists(settingsPath))
                {
                    return AppSettings.Default();
                }

                var jsonContent = File.ReadAllText(settingsPath, Encoding.UTF8);
                var settings = JsonConvert.DeserializeObject<AppSettings>(jsonContent);
                
                return settings ?? AppSettings.Default();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载设置失败: {ex.Message}");
                return AppSettings.Default();
            }
        }

        /// <summary>
        /// 保存应用设置
        /// </summary>
        /// <param name="settings">应用设置对象</param>
        /// <returns>是否保存成功</returns>
        public bool SaveSettings(AppSettings settings)
        {
            try
            {
                var settingsPath = Path.Combine(_context.FilesDir?.AbsolutePath ?? "", _settingsFileName);
                
                var jsonContent = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(settingsPath, jsonContent, Encoding.UTF8);
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存设置失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 配置数据模型
        /// </summary>
        private class ConfigData
        {
            [JsonProperty("version")]
            public string Version { get; set; } = string.Empty;

            [JsonProperty("used_items")]
            public Dictionary<string, List<string>> UsedItems { get; set; } = new Dictionary<string, List<string>>();
        }
    }
}