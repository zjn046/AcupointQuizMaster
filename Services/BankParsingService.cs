using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AcupointQuizMaster.Models;
using System.Text;
using System.Linq;
using Android.Content;
using Java.IO;

namespace AcupointQuizMaster.Services
{
    /// <summary>
    /// 题库解析服务 - 从Assets目录读取txt文件
    /// </summary>
    public class BankParsingService
    {
        private static readonly string[] PosLabels = { "定位", "取穴", "位置" };
        private static readonly string[] ZhuLabels = { "主治", "主治病症", "主治病证", "主治功能", "功效", "作用", "适应症" };
        private static readonly string[] SpecLabels = { "特定穴", "类别", "属性", "所属" };
        private static readonly Regex MeridianRegex = new Regex(@"^\s*#+\s*(\S.*\S|\S)\s*$", RegexOptions.Compiled);

        private readonly Context _context;

        // 文件名到资源ID的映射
        private static readonly Dictionary<string, int> FileNameToResourceId = new Dictionary<string, int>
        {
            ["bank_01_lung.txt"] = Resource.Raw.lung_meridian,
            ["bank_02_large_intestine.txt"] = Resource.Raw.large_intestine_meridian,
            ["bank_03_stomach.txt"] = Resource.Raw.stomach_meridian,
            ["bank_04_spleen.txt"] = Resource.Raw.spleen_meridian,
            ["bank_05_heart.txt"] = Resource.Raw.heart_meridian,
            ["bank_06_small_intestine.txt"] = Resource.Raw.small_intestine_meridian,
            ["bank_07_bladder.txt"] = Resource.Raw.bladder_meridian,
            ["bank_08_kidney.txt"] = Resource.Raw.kidney_meridian,
            ["bank_09_pericardium.txt"] = Resource.Raw.pericardium_meridian,
            ["bank_10_sanjiao.txt"] = Resource.Raw.sanjiao_meridian,
            ["bank_11_gallbladder.txt"] = Resource.Raw.gallbladder_meridian,
            ["bank_12_liver.txt"] = Resource.Raw.liver_meridian,
            ["bank_13_du.txt"] = Resource.Raw.du_mai,
            ["bank_14_ren.txt"] = Resource.Raw.ren_mai,
            ["bank_15_extra.txt"] = Resource.Raw.extra_points,
            ["bank_16_exam.txt"] = Resource.Raw.exam_essentials
        };
        private static readonly Dictionary<string, string> FileNameToChineseName = new Dictionary<string, string>
        {
            ["bank_01_lung.txt"] = "手太阴肺经",
            ["bank_02_large_intestine.txt"] = "手阳明大肠经",
            ["bank_03_stomach.txt"] = "足阳明胃经",
            ["bank_04_spleen.txt"] = "足太阴脾经",
            ["bank_05_heart.txt"] = "手少阴心经",
            ["bank_06_small_intestine.txt"] = "手太阳小肠经",
            ["bank_07_bladder.txt"] = "足太阳膀胱经",
            ["bank_08_kidney.txt"] = "足少阴肾经",
            ["bank_09_pericardium.txt"] = "手厥阴心包经",
            ["bank_10_sanjiao.txt"] = "手少阳三焦经",
            ["bank_11_gallbladder.txt"] = "足少阳胆经",
            ["bank_12_liver.txt"] = "足厥阴肝经",
            ["bank_13_du.txt"] = "督脉",
            ["bank_14_ren.txt"] = "任脉",
            ["bank_15_extra.txt"] = "经外奇穴",
            ["bank_16_exam.txt"] = "盲医考必背"
        };

        public BankParsingService(Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 解析题库文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>解析结果</returns>
        public BankInfo ParseBank(string fileName)
        {
            try
            {
                var text = LoadTextFromAssets(fileName);
                return ParseBankContent(text, fileName);
            }
            catch (Exception ex)
            {
                throw new Exception($"解析题库文件 {fileName} 失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从Resources目录读取文本内容
        /// </summary>
        private string LoadTextFromAssets(string fileName)
        {
            try
            {
                if (!FileNameToResourceId.TryGetValue(fileName, out var resourceId))
                    throw new System.IO.FileNotFoundException($"找不到对应的资源文件: {fileName}");

                using var stream = _context.Resources?.OpenRawResource(resourceId);
                if (stream == null)
                    throw new System.IO.FileNotFoundException($"找不到资源文件: {fileName}");

                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                throw new Exception($"读取资源文件 {fileName} 失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 解析题库内容
        /// </summary>
        private BankInfo ParseBankContent(string text, string fileName)
        {
            var bankInfo = new BankInfo
            {
                FileName = fileName,
                Name = FileNameToChineseName.GetValueOrDefault(fileName, Path.GetFileNameWithoutExtension(fileName))
            };

            // 规范化换行符并分割行
            var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            
            string currentMeridian = string.Empty;
            int i = 0;
            
            while (i < lines.Length)
            {
                var line = lines[i].Trim();
                
                // 跳过空行
                if (string.IsNullOrEmpty(line))
                {
                    i++;
                    continue;
                }

                // 检查是否是经络标题（#开头的行）
                var meridianMatch = MeridianRegex.Match(line);
                if (meridianMatch.Success)
                {
                    bankInfo.HasMeridianHeaders = true;
                    currentMeridian = StripTrailingColon(meridianMatch.Groups[1].Value);
                    
                    // 查找下一个非空非#行作为映射
                    var nextName = FindNextAcupointName(lines, i + 1);
                    if (!string.IsNullOrEmpty(nextName))
                    {
                        bankInfo.HeaderToNext[StripTrailingColon(line)] = nextName;
                    }
                    
                    i++;
                    continue;
                }

                // 处理穴位条目
                var acupointName = StripTrailingColon(line);
                var bodyLines = new List<string>();
                i++;
                
                // 读取穴位的详细信息直到空行或下一个#行
                while (i < lines.Length)
                {
                    var bodyLine = lines[i];
                    var trimmedBodyLine = bodyLine.Trim();
                    
                    if (string.IsNullOrEmpty(trimmedBodyLine))
                    {
                        i++;
                        break;
                    }
                    
                    if (MeridianRegex.IsMatch(trimmedBodyLine))
                    {
                        // 不消耗#行，让外层循环处理
                        break;
                    }
                    
                    bodyLines.Add(bodyLine.TrimEnd());
                    i++;
                }

                // 解析穴位详细信息
                var acupointInfo = ParseAcupointInfo(acupointName, bodyLines, currentMeridian);
                
                if (!bankInfo.AcupointNames.Contains(acupointName))
                {
                    bankInfo.AcupointNames.Add(acupointName);
                }
                
                bankInfo.AcupointDetails[acupointName] = acupointInfo;
            }

            return bankInfo;
        }

        /// <summary>
        /// 查找下一个穴位名称
        /// </summary>
        private string FindNextAcupointName(string[] lines, int startIndex)
        {
            for (int j = startIndex; j < lines.Length; j++)
            {
                var line = lines[j].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (MeridianRegex.IsMatch(line)) break;
                
                return StripTrailingColon(line);
            }
            return string.Empty;
        }

        /// <summary>
        /// 解析穴位信息
        /// </summary>
        private AcupointInfo ParseAcupointInfo(string name, List<string> bodyLines, string meridian)
        {
            var acupointInfo = new AcupointInfo
            {
                Name = name,
                Meridian = meridian
            };

            if (bodyLines.Count == 0)
                return acupointInfo;

            // 将所有内容合并
            var fullText = string.Join("\n", bodyLines.Select(line => CleanHtml(line.Trim())));

            // 提取各个部分
            var (location, treatment, specialType, method) = ExtractSections(fullText);

            acupointInfo.Location = location ?? string.Empty;
            acupointInfo.Treatment = treatment ?? string.Empty;
            acupointInfo.SpecialType = specialType ?? string.Empty;
            acupointInfo.Method = method ?? string.Empty;

            return acupointInfo;
        }

        /// <summary>
        /// 提取各个部分的内容
        /// </summary>
        private (string?, string?, string?, string?) ExtractSections(string text)
        {
            var posMatch = CreateSectionRegex(PosLabels, ZhuLabels.Concat(SpecLabels).ToArray()).Match(text);
            var zhuMatch = CreateSectionRegex(ZhuLabels, PosLabels.Concat(SpecLabels).ToArray()).Match(text);
            var specMatch = CreateSectionRegex(SpecLabels, PosLabels.Concat(ZhuLabels).ToArray()).Match(text);
            
            // 简单的取穴方法提取（如果包含"取穴"关键字）
            var methodMatch = Regex.Match(text, @"取穴[：:]?\s*([^取穴定位主治特定穴]+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            var pos = posMatch.Success ? NormalizeText(posMatch.Groups[1].Value) : null;
            var zhu = zhuMatch.Success ? NormalizeText(zhuMatch.Groups[1].Value) : null;
            var spec = specMatch.Success ? NormalizeText(specMatch.Groups[1].Value) : null;
            var method = methodMatch.Success ? NormalizeText(methodMatch.Groups[1].Value) : null;

            return (pos, zhu, spec, method);
        }

        /// <summary>
        /// 创建用于提取部分内容的正则表达式
        /// </summary>
        private Regex CreateSectionRegex(string[] targetLabels, string[] otherLabels)
        {
            var targetPattern = string.Join("|", targetLabels.Select(Regex.Escape));
            var otherPattern = string.Join("|", targetLabels.Concat(otherLabels).Select(Regex.Escape));
            
            var pattern = $@"^\s*(?:[【\[\(]?\s*(?:{targetPattern})\s*[】\]\)]?\s*[:：]?\s*)(.*?)(?=^\s*(?:[【\[\(]?\s*(?:{otherPattern})\s*[】\]\)]?\s*[:：]?\s*)|\Z)";
            
            return new Regex(pattern, RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// 规范化文本
        /// </summary>
        private string? NormalizeText(string? text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            
            var lines = text.Replace("\r", "").Split('\n')
                           .Select(line => line.Trim())
                           .ToList();
            
            var cleaned = new List<string>();
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line) && cleaned.Count > 0 && string.IsNullOrEmpty(cleaned.Last()))
                    continue;
                cleaned.Add(line);
            }
            
            var result = string.Join("\n", cleaned).Trim();
            return string.IsNullOrEmpty(result) ? null : result;
        }

        /// <summary>
        /// 清理HTML标签
        /// </summary>
        private string CleanHtml(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            text = text.Replace("<br />", "\n")
                      .Replace("<br>", "\n")
                      .Replace("<br/>", "\n");
            
            text = Regex.Replace(text, @"<[^>]+>", "");
            
            return text;
        }

        /// <summary>
        /// 去掉末尾的冒号
        /// </summary>
        private string StripTrailingColon(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            text = text.Trim();
            if (text.EndsWith("：") || text.EndsWith(":"))
            {
                text = text.Substring(0, text.Length - 1).Trim();
            }
            
            return text;
        }

        /// <summary>
        /// 获取所有可用的题库文件
        /// </summary>
        public List<string> GetAvailableBankFiles()
        {
            try
            {
                // 直接返回所有预定义的文件列表，因为这些文件已经内置在Resources中
                return FileNameToChineseName.Keys.OrderBy(x => x).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"获取题库文件列表失败: {ex.Message}", ex);
            }
        }
    }
}