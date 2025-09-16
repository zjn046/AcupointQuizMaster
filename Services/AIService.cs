using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using AcupointQuizMaster.Models;
using System.Text.RegularExpressions;
using System.Linq;

namespace AcupointQuizMaster.Services
{
    /// <summary>
    /// AI服务类 - 处理AI出题和判卷功能
    /// </summary>
    public class AIService : IDisposable
    {
        private const string DefaultApiUrl = "https://api.deepseek.com/v1/chat/completions";
        private const string DefaultModelName = "deepseek-chat";
        
        private readonly HttpClient _httpClient;
        private string _apiKey;
        private string _apiUrl;
        private string _modelName;

        public AIService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(45);
            
            // 初始化默认设置
            var defaultSettings = DefaultApiConfig.GetDefaultSettings();
            _apiKey = defaultSettings.ApiKey;
            _apiUrl = defaultSettings.ApiUrl;
            _modelName = defaultSettings.ModelName;
        }

        /// <summary>
        /// 更新API设置
        /// </summary>
        /// <param name="settings">应用设置</param>
        public void UpdateSettings(AppSettings settings)
        {
            if (settings != null && settings.IsValid())
            {
                _apiKey = settings.ApiKey;
                _apiUrl = settings.ApiUrl;
                _modelName = settings.ModelName;
            }
        }

        /// <summary>
        /// AI出题 - 强制按指定维度出题
        /// </summary>
        /// <param name="acupointName">穴位名称</param>
        /// <param name="acupointInfo">穴位信息</param>
        /// <param name="forcedQuestionType">强制题型</param>
        /// <returns>题目、标准答案和题型</returns>
        public async Task<(string Question, string CanonicalAnswer, string QuestionType)> BuildQuestionForcedAsync(
            string acupointName, AcupointInfo acupointInfo, string forcedQuestionType)
        {
            try
            {
                var fields = acupointInfo.GetAllFields();
                if (fields.Count == 0)
                {
                    return ($"关于【{acupointName}】的{forcedQuestionType}？", "（题库缺少资料）", forcedQuestionType);
                }

                var systemMessage = "你是中医针灸测评出题官。请严格按指定维度(forced_q_type)出题：" +
                                   "仅输出 JSON：{\\\"question\\\":string,\\\"canonical_answer\\\":string,\\\"q_type\\\":string}；" +
                                   "其中 q_type 必须等于 forced_q_type。题目需包含穴位名与维度。" +
                                   "答案以题库原文为准，可以做必要整合，但不得凭空加入未提供信息。";

                var request = new AIQuestionRequest
                {
                    AcupointName = acupointName,
                    ForcedQuestionType = forcedQuestionType,
                    CorrespondingText = fields.GetValueOrDefault(forcedQuestionType, ""),
                    Materials = fields
                };

                var response = await CallDeepSeekAsync(systemMessage, request, responseJson: true, temperature: 0.35);
                var content = ExtractContent(response);

                var questionResponse = ParseJsonResponse<AIQuestionResponse>(content);
                
                var questionType = questionResponse?.QuestionType ?? forcedQuestionType;
                var question = questionResponse?.Question ?? $"关于【{acupointName}】的{questionType}？";
                
                // 确保题目包含穴位名
                if (!question.Contains(acupointName))
                {
                    question = $"关于【{acupointName}】的{questionType}？";
                }
                
                var canonicalAnswer = questionResponse?.CanonicalAnswer ?? 
                                     (fields.ContainsKey(questionType) ? fields[questionType] : "") ?? 
                                     fields.Values.FirstOrDefault() ?? "";

                return (question, canonicalAnswer, questionType);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AI出题失败: {ex.Message}");
                // 返回兜底结果
                var fields = acupointInfo.GetAllFields();
                var answer = (fields.ContainsKey(forcedQuestionType) ? fields[forcedQuestionType] : "") ?? 
                            fields.Values.FirstOrDefault() ?? "（无相关资料）";
                return ($"关于【{acupointName}】的{forcedQuestionType}？", answer, forcedQuestionType);
            }
        }

        /// <summary>
        /// AI判卷
        /// </summary>
        /// <param name="question">题目</param>
        /// <param name="userAnswer">用户答案</param>
        /// <param name="acupointName">穴位名称</param>
        /// <param name="questionType">题型</param>
        /// <param name="acupointInfo">穴位信息</param>
        /// <returns>判卷结果</returns>
        public async Task<AIGradeResponse> GradeAnswerAsync(
            string question, string userAnswer, string acupointName, 
            string questionType, AcupointInfo acupointInfo)
        {
            try
            {
                var fields = acupointInfo.GetAllFields();
                var standardAnswer = fields.ContainsKey(questionType) ? fields[questionType] : "";

                // 本地同音字检查（简化版）
                if (IsPinyinMatch(userAnswer, standardAnswer))
                {
                    return new AIGradeResponse
                    {
                        Score = 100.0,
                        Pass = true,
                        Feedback = "回答正确。",
                        ModelAnswer = standardAnswer,
                        IncorrectReason = "",
                        Subscores = new AIGradeSubscores
                        {
                            Accuracy = 5.0,
                            Coverage = 5.0,
                            KeyTerms = 5.0,
                            Specificity = 5.0,
                            Clarity = 5.0
                        }
                    };
                }

                var systemMessage = "你是中医针灸测评判卷官。只输出 JSON：" +
                                   "{\"subscores\":{\"accuracy\":number,\"coverage\":number,\"key_terms\":number,\"specificity\":number,\"clarity\":number}," +
                                   "\"score\":number,\"pass\":boolean,\"feedback\":string,\"model_answer\":string,\"incorrect_reason\":string}。" +
                                   "评分 0~100，≥80 通过。严格依据提供的题库资料与指定的 q_type，不得凭空扩展或改题。" +
                                   "评分方法：在以下 5 个维度各打 0~5 分（允许 0.5 分）：" +
                                   "accuracy=与标准答案含义一致性（权重40%）；" +
                                   "coverage=关键要点覆盖度（25%）；" +
                                   "key_terms=术语/名称正确性（15%）；" +
                                   "specificity=细节与限定（10%）；" +
                                   "clarity=表达清晰与条理（10%）。" +
                                   "总分 = (accuracy*0.4 + coverage*0.25 + key_terms*0.15 + specificity*0.10 + clarity*0.10) * 20，" +
                                   "并四舍五入到 1 位小数。务必使用整个分数区间，合理给出部分分；空答案给 0 分。" +
                                   "若用户答案与标准答案在无声调拼音上一致或等价，应按完全正确评分，并在反馈中不要提及拼音/同音/读音等。" +
                                   "当判为不通过时，请在 incorrect_reason 中简要列出缺失的具体要点；通过时留空。";

                var request = new AIGradeRequest
                {
                    Question = question,
                    UserAnswer = userAnswer,
                    AcupointName = acupointName,
                    QuestionType = questionType,
                    StandardAnswer = standardAnswer,
                    BankMaterials = fields
                };

                var response = await CallDeepSeekAsync(systemMessage, request, responseJson: true, temperature: 0.0);
                var content = ExtractContent(response);

                var gradeResponse = ParseJsonResponse<AIGradeResponse>(content);
                
                if (gradeResponse == null)
                {
                    // 返回兜底结果
                    return CreateFallbackGradeResponse(userAnswer, standardAnswer);
                }

                // 重新计算总分（如果有维度分数）
                if (gradeResponse.Subscores != null)
                {
                    var subscores = gradeResponse.Subscores;
                    var calculatedScore = (subscores.Accuracy * 0.4 + subscores.Coverage * 0.25 + 
                                         subscores.KeyTerms * 0.15 + subscores.Specificity * 0.10 + 
                                         subscores.Clarity * 0.10) * 20.0;
                    gradeResponse.Score = Math.Round(calculatedScore, 1);
                    gradeResponse.Pass = gradeResponse.Score >= 80.0;
                }

                // 清理反馈中的同音字相关字样
                gradeResponse.Feedback = ScrubHomophoneTerms(gradeResponse.Feedback);

                // 补充不正确原因
                if (!gradeResponse.Pass && string.IsNullOrWhiteSpace(gradeResponse.IncorrectReason))
                {
                    gradeResponse.IncorrectReason = GenerateIncorrectReason(userAnswer, standardAnswer);
                }

                return gradeResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AI判卷失败: {ex.Message}");
                // 返回兜底判卷结果
                var fields = acupointInfo.GetAllFields();
                return CreateFallbackGradeResponse(userAnswer, (fields.ContainsKey(questionType) ? fields[questionType] : ""));
            }
        }

        /// <summary>
        /// 调用DeepSeek API
        /// </summary>
        private async Task<dynamic> CallDeepSeekAsync(string systemMessage, object userContent, bool responseJson = true, double temperature = 0.2)
        {
            var messages = new[]
            {
                new { role = "system", content = systemMessage },
                new { role = "user", content = JsonConvert.SerializeObject(userContent, Formatting.None) }
            };

            var payload = new
            {
                model = _modelName,
                messages = messages,
                temperature = temperature,
                response_format = responseJson ? new { type = "json_object" } : null
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsync(_apiUrl, content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API调用失败: {response.StatusCode} - {responseText}");
            }

            return JsonConvert.DeserializeObject(responseText) ?? new { };
        }

        /// <summary>
        /// 从API响应中提取内容
        /// </summary>
        private string ExtractContent(dynamic response)
        {
            try
            {
                return response?.choices?[0]?.message?.content?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 解析JSON响应
        /// </summary>
        private T? ParseJsonResponse<T>(string content) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch
            {
                // 尝试从内容中提取JSON
                var match = Regex.Match(content, @"\{.*\}", RegexOptions.Singleline);
                if (match.Success)
                {
                    try
                    {
                        return JsonConvert.DeserializeObject<T>(match.Value);
                    }
                    catch
                    {
                        // 忽略解析错误
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// 简化版拼音匹配检查
        /// </summary>
        private bool IsPinyinMatch(string userAnswer, string canonicalAnswer)
        {
            if (string.IsNullOrWhiteSpace(userAnswer) || string.IsNullOrWhiteSpace(canonicalAnswer))
                return false;

            var userChinese = ExtractChinese(userAnswer);
            var canonicalChinese = ExtractChinese(canonicalAnswer);

            if (string.IsNullOrEmpty(canonicalChinese) || canonicalChinese.Length > 8)
                return false;

            // 简单的字符包含检查
            return userChinese.Contains(canonicalChinese) || canonicalChinese.Contains(userChinese);
        }

        /// <summary>
        /// 提取中文字符
        /// </summary>
        private string ExtractChinese(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return new string(text.Where(ch => ch >= '\u4e00' && ch <= '\u9fff').ToArray());
        }

        /// <summary>
        /// 清理同音字相关字样
        /// </summary>
        private string ScrubHomophoneTerms(string feedback)
        {
            if (string.IsNullOrEmpty(feedback)) return feedback;

            var badWords = new[] { "同音", "拼音", "读音", "谐音", "发音", "homophone", "pinyin" };
            var result = feedback;

            foreach (var word in badWords)
            {
                result = result.Replace(word, "");
            }

            result = Regex.Replace(result, @"[（(]\s*[)）]", "");
            result = Regex.Replace(result, @"\s{2,}", " ");

            return string.IsNullOrWhiteSpace(result) ? "回答正确。" : result.Trim();
        }

        /// <summary>
        /// 生成不正确的原因
        /// </summary>
        private string GenerateIncorrectReason(string userAnswer, string standardAnswer)
        {
            if (string.IsNullOrWhiteSpace(userAnswer))
                return "空答案或无效输入";

            var userChinese = ExtractChinese(userAnswer);
            var standardChinese = ExtractChinese(standardAnswer);

            if (!string.IsNullOrEmpty(standardChinese) && !string.IsNullOrEmpty(userChinese) &&
                !userChinese.Any(ch => standardChinese.Contains(ch)))
            {
                return "缺少关键要点或用词完全不符";
            }

            return "与标准答案含义不符或要点不全";
        }

        /// <summary>
        /// 创建兜底判卷结果
        /// </summary>
        private AIGradeResponse CreateFallbackGradeResponse(string userAnswer, string standardAnswer)
        {
            // 使用简单的相似度计算
            var similarity = CalculateSimpleSimilarity(userAnswer, standardAnswer);
            var score = Math.Round(similarity * 100, 1);
            var pass = score >= 80.0;

            return new AIGradeResponse
            {
                Score = score,
                Pass = pass,
                Feedback = pass ? "回答正确。" : "答案与标准答案存在较大差异。",
                ModelAnswer = standardAnswer,
                IncorrectReason = pass ? "" : "与标准答案含义不符或要点不全",
                Subscores = new AIGradeSubscores
                {
                    Accuracy = similarity * 5,
                    Coverage = similarity * 5,
                    KeyTerms = similarity * 5,
                    Specificity = similarity * 5,
                    Clarity = similarity * 5
                }
            };
        }

        /// <summary>
        /// 计算简单相似度
        /// </summary>
        private double CalculateSimpleSimilarity(string text1, string text2)
        {
            if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
                return 0.0;

            var cleanText1 = Regex.Replace(text1.ToLower(), @"\s+", "");
            var cleanText2 = Regex.Replace(text2.ToLower(), @"\s+", "");

            if (cleanText1 == cleanText2) return 1.0;

            var commonChars = cleanText1.Intersect(cleanText2).Count();
            var totalChars = Math.Max(cleanText1.Length, cleanText2.Length);

            return totalChars > 0 ? (double)commonChars / totalChars : 0.0;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}