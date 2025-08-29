using System.Collections.Generic;

namespace AcupointQuizMaster.Models
{
    /// <summary>
    /// 穴位信息模型类
    /// </summary>
    public class AcupointInfo
    {
        /// <summary>
        /// 穴位名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 定位信息
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// 主治功能
        /// </summary>
        public string Treatment { get; set; } = string.Empty;

        /// <summary>
        /// 特定穴分类
        /// </summary>
        public string SpecialType { get; set; } = string.Empty;

        /// <summary>
        /// 归经信息
        /// </summary>
        public string Meridian { get; set; } = string.Empty;

        /// <summary>
        /// 取穴方法
        /// </summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// 获取所有非空字段的字典
        /// </summary>
        /// <returns>字段名到内容的映射</returns>
        public Dictionary<string, string> GetAllFields()
        {
            var fields = new Dictionary<string, string>();
            
            if (!string.IsNullOrWhiteSpace(Location))
                fields["定位"] = Location.Trim();
            
            if (!string.IsNullOrWhiteSpace(Treatment))
                fields["主治"] = Treatment.Trim();
                
            if (!string.IsNullOrWhiteSpace(SpecialType))
                fields["特定穴"] = SpecialType.Trim();
                
            if (!string.IsNullOrWhiteSpace(Meridian))
                fields["归经"] = Meridian.Trim();
                
            if (!string.IsNullOrWhiteSpace(Method))
                fields["取穴"] = Method.Trim();

            return fields;
        }
    }
}