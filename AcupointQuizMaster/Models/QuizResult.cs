namespace AcupointQuizMaster.Models
{
    /// <summary>
    /// 测验结果模型类
    /// </summary>
    public class QuizResult
    {
        /// <summary>
        /// 总题数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 答对数
        /// </summary>
        public int CorrectCount { get; set; }

        /// <summary>
        /// 答错数
        /// </summary>
        public int WrongCount { get; set; }

        /// <summary>
        /// 正确率（百分比）
        /// </summary>
        public double AccuracyPercent
        {
            get
            {
                if (TotalCount <= 0) return 0;
                return Math.Round((double)CorrectCount / TotalCount * 100, 2);
            }
        }

        /// <summary>
        /// 用时（毫秒）
        /// </summary>
        public long ElapsedMilliseconds { get; set; }

        /// <summary>
        /// 题库名称
        /// </summary>
        public string BankName { get; set; } = string.Empty;

        /// <summary>
        /// 答错的穴位列表
        /// </summary>
        public List<string> WrongItems { get; set; } = new List<string>();

        /// <summary>
        /// 格式化用时显示
        /// </summary>
        public string FormattedTime
        {
            get
            {
                if (ElapsedMilliseconds <= 0) return "0分00秒";
                
                var totalSeconds = ElapsedMilliseconds / 1000;
                var minutes = totalSeconds / 60;
                var seconds = totalSeconds % 60;
                
                return $"{minutes}分{seconds:00}秒";
            }
        }

        /// <summary>
        /// 格式化正确率显示
        /// </summary>
        public string FormattedAccuracy
        {
            get
            {
                var percent = AccuracyPercent;
                var percentStr = percent.ToString("0.##");
                return percentStr + "%";
            }
        }
    }
}