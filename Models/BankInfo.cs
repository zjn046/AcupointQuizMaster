using System;
using System.Collections.Generic;

namespace AcupointQuizMaster.Models
{
    /// <summary>
    /// 题库信息模型类
    /// </summary>
    public class BankInfo
    {
        /// <summary>
        /// 题库名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 题库文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 穴位名称列表（去除标题行）
        /// </summary>
        public List<string> AcupointNames { get; set; } = new List<string>();

        /// <summary>
        /// 穴位详细信息字典
        /// </summary>
        public Dictionary<string, AcupointInfo> AcupointDetails { get; set; } = new Dictionary<string, AcupointInfo>();

        /// <summary>
        /// 是否包含经络标题
        /// </summary>
        public bool HasMeridianHeaders { get; set; }

        /// <summary>
        /// 标题到下一个穴位的映射
        /// </summary>
        public Dictionary<string, string> HeaderToNext { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 获取剩余可抽取的穴位（排除已用过的）
        /// </summary>
        /// <param name="usedItems">已使用的穴位集合</param>
        /// <returns>可用的穴位名称列表</returns>
        public List<string> GetRemainingItems(HashSet<string> usedItems)
        {
            var remaining = new List<string>();
            foreach (var name in AcupointNames)
            {
                if (!usedItems.Contains(name))
                {
                    remaining.Add(name);
                }
            }
            return remaining;
        }

        /// <summary>
        /// 获取穴位总数
        /// </summary>
        public int TotalCount => AcupointNames.Count;
    }
}