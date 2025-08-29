using Newtonsoft.Json;

namespace AcupointQuizMaster.Models
{
    /// <summary>
    /// AI测评相关的模型类
    /// </summary>
    
    /// <summary>
    /// AI出题请求模型
    /// </summary>
    public class AIQuestionRequest
    {
        [JsonProperty("穴位名")]
        public string AcupointName { get; set; } = string.Empty;

        [JsonProperty("forced_q_type")]
        public string ForcedQuestionType { get; set; } = string.Empty;

        [JsonProperty("对应文本")]
        public string CorrespondingText { get; set; } = string.Empty;

        [JsonProperty("资料")]
        public Dictionary<string, string> Materials { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// AI出题响应模型
    /// </summary>
    public class AIQuestionResponse
    {
        [JsonProperty("question")]
        public string Question { get; set; } = string.Empty;

        [JsonProperty("canonical_answer")]
        public string CanonicalAnswer { get; set; } = string.Empty;

        [JsonProperty("q_type")]
        public string QuestionType { get; set; } = string.Empty;
    }

    /// <summary>
    /// AI判卷请求模型
    /// </summary>
    public class AIGradeRequest
    {
        [JsonProperty("question")]
        public string Question { get; set; } = string.Empty;

        [JsonProperty("user_answer")]
        public string UserAnswer { get; set; } = string.Empty;

        [JsonProperty("穴位名")]
        public string AcupointName { get; set; } = string.Empty;

        [JsonProperty("q_type")]
        public string QuestionType { get; set; } = string.Empty;

        [JsonProperty("标准答案")]
        public string StandardAnswer { get; set; } = string.Empty;

        [JsonProperty("题库资料")]
        public Dictionary<string, string> BankMaterials { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// AI判卷响应的维度分数模型
    /// </summary>
    public class AIGradeSubscores
    {
        [JsonProperty("accuracy")]
        public double Accuracy { get; set; }

        [JsonProperty("coverage")]
        public double Coverage { get; set; }

        [JsonProperty("key_terms")]
        public double KeyTerms { get; set; }

        [JsonProperty("specificity")]
        public double Specificity { get; set; }

        [JsonProperty("clarity")]
        public double Clarity { get; set; }
    }

    /// <summary>
    /// AI判卷响应模型
    /// </summary>
    public class AIGradeResponse
    {
        [JsonProperty("subscores")]
        public AIGradeSubscores? Subscores { get; set; }

        [JsonProperty("score")]
        public double Score { get; set; }

        [JsonProperty("pass")]
        public bool Pass { get; set; }

        [JsonProperty("feedback")]
        public string Feedback { get; set; } = string.Empty;

        [JsonProperty("model_answer")]
        public string ModelAnswer { get; set; } = string.Empty;

        [JsonProperty("incorrect_reason")]
        public string IncorrectReason { get; set; } = string.Empty;
    }
}