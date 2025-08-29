using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using AcupointQuizMaster.Models;

namespace AcupointQuizMaster.Services
{
    /// <summary>
    /// 测验管理器 - 处理测验逻辑
    /// </summary>
    public class QuizManager
    {
        private readonly PersistenceService _persistenceService;
        private BankInfo? _currentBank;
        private Queue<string> _currentQueue = new Queue<string>();
        private List<string> _correctAnswers = new List<string>();
        private List<string> _wrongAnswers = new List<string>();
        private HashSet<string> _sessionMarkedItems = new HashSet<string>();
        private HashSet<string> _transientUsedItems = new HashSet<string>();
        private Dictionary<string, HashSet<string>> _allUsedItems = new Dictionary<string, HashSet<string>>();
        private Stopwatch? _stopwatch;
        private long _elapsedMilliseconds;
        private int _totalCount;
        private int _currentIndex;
        private string? _currentItem;
        private bool _hasAnyAnswer;

        public QuizManager(PersistenceService persistenceService)
        {
            _persistenceService = persistenceService;
            _allUsedItems = _persistenceService.LoadUsedItems();
        }

        /// <summary>
        /// 当前题库
        /// </summary>
        public BankInfo? CurrentBank => _currentBank;

        /// <summary>
        /// 当前题目
        /// </summary>
        public string? CurrentItem => _currentItem;

        /// <summary>
        /// 总题数
        /// </summary>
        public int TotalCount => _totalCount;

        /// <summary>
        /// 当前题目索引
        /// </summary>
        public int CurrentIndex => _currentIndex;

        /// <summary>
        /// 答对数
        /// </summary>
        public int CorrectCount => _correctAnswers.Count;

        /// <summary>
        /// 答错数
        /// </summary>
        public int WrongCount => _wrongAnswers.Count;

        /// <summary>
        /// 是否有任何答题记录
        /// </summary>
        public bool HasAnyAnswer => _hasAnyAnswer;

        /// <summary>
        /// 是否测验已结束
        /// </summary>
        public bool IsQuizFinished => _currentQueue.Count == 0 && _currentItem == null;

        /// <summary>
        /// 设置当前题库
        /// </summary>
        /// <param name="bankInfo">题库信息</param>
        public void SetCurrentBank(BankInfo bankInfo)
        {
            _currentBank = bankInfo;
            ResetQuiz();
        }

        /// <summary>
        /// 开始新的测验批次
        /// </summary>
        /// <param name="quantity">题目数量</param>
        /// <returns>是否开始成功</returns>
        public bool StartQuiz(int quantity)
        {
            if (_currentBank == null) 
                return false;

            var usedItems = GetEffectiveUsedItems(_currentBank.FileName);
            var remainingItems = _currentBank.GetRemainingItems(usedItems);

            if (remainingItems.Count == 0)
            {
                return false; // 没有可用题目
            }

            var actualQuantity = Math.Min(quantity, remainingItems.Count);
            
            // 随机选择题目
            var random = new Random();
            var selectedItems = remainingItems.OrderBy(x => random.Next()).Take(actualQuantity).ToList();

            ResetQuiz();
            _totalCount = actualQuantity;
            
            foreach (var item in selectedItems)
            {
                _currentQueue.Enqueue(item);
            }

            _stopwatch = Stopwatch.StartNew();
            return true;
        }

        /// <summary>
        /// 获取下一个题目
        /// </summary>
        /// <returns>下一个题目名称，如果没有则返回null</returns>
        public string? GetNextItem()
        {
            if (_currentQueue.Count > 0)
            {
                _currentItem = _currentQueue.Dequeue();
                _currentIndex++;
                return _currentItem;
            }

            _currentItem = null;
            return null;
        }

        /// <summary>
        /// 标记当前题目为正确
        /// </summary>
        /// <returns>是否标记成功</returns>
        public bool MarkCorrect()
        {
            if (_currentItem == null) return false;

            _correctAnswers.Add(_currentItem);
            MarkCurrentItemAsUsed();
            _hasAnyAnswer = true;
            _currentItem = null;

            return true;
        }

        /// <summary>
        /// 标记当前题目为错误
        /// </summary>
        /// <returns>是否标记成功</returns>
        public bool MarkWrong()
        {
            if (_currentItem == null) return false;

            _wrongAnswers.Add(_currentItem);
            MarkCurrentItemAsUsed();
            _hasAnyAnswer = true;
            _currentItem = null;

            return true;
        }

        /// <summary>
        /// 完成测验
        /// </summary>
        public void FinishQuiz()
        {
            _stopwatch?.Stop();
            _elapsedMilliseconds = _stopwatch?.ElapsedMilliseconds ?? 0;
        }

        /// <summary>
        /// 获取测验结果
        /// </summary>
        /// <returns>测验结果</returns>
        public QuizResult GetQuizResult()
        {
            return new QuizResult
            {
                TotalCount = _totalCount,
                CorrectCount = _correctAnswers.Count,
                WrongCount = _wrongAnswers.Count,
                ElapsedMilliseconds = _elapsedMilliseconds,
                BankName = _currentBank?.Name ?? "未知题库",
                WrongItems = new List<string>(_wrongAnswers)
            };
        }

        /// <summary>
        /// 获取错误题目的详细信息
        /// </summary>
        /// <returns>错误题目详细信息列表</returns>
        public List<(string Name, AcupointInfo Info)> GetWrongItemsDetails()
        {
            var wrongDetails = new List<(string, AcupointInfo)>();
            
            if (_currentBank != null)
            {
                foreach (var wrongItem in _wrongAnswers)
                {
                    if (_currentBank.AcupointDetails.TryGetValue(wrongItem, out var info))
                    {
                        wrongDetails.Add((wrongItem, info));
                    }
                }
            }

            return wrongDetails;
        }

        /// <summary>
        /// 保存进度
        /// </summary>
        /// <returns>是否保存成功</returns>
        public bool SaveProgress()
        {
            if (_currentBank == null || !_hasAnyAnswer || _sessionMarkedItems.Count == 0)
                return true; // 没有需要保存的内容

            var bankKey = _currentBank.FileName;
            
            if (!_allUsedItems.ContainsKey(bankKey))
            {
                _allUsedItems[bankKey] = new HashSet<string>();
            }

            // 合并已标记的项目
            foreach (var item in _sessionMarkedItems)
            {
                _allUsedItems[bankKey].Add(item);
            }

            return _persistenceService.SaveUsedItems(_allUsedItems);
        }

        /// <summary>
        /// 清空指定题库的使用记录
        /// </summary>
        /// <param name="bankFileName">题库文件名</param>
        /// <returns>是否清空成功</returns>
        public bool ClearUsedItems(string bankFileName)
        {
            if (_allUsedItems.ContainsKey(bankFileName))
            {
                _allUsedItems[bankFileName].Clear();
            }

            if (_currentBank != null && _currentBank.FileName == bankFileName)
            {
                _transientUsedItems.Clear();
            }

            return _persistenceService.SaveUsedItems(_allUsedItems);
        }

        /// <summary>
        /// 获取题库统计信息
        /// </summary>
        /// <param name="bankInfo">题库信息</param>
        /// <returns>统计信息字符串</returns>
        public string GetBankStats(BankInfo bankInfo)
        {
            var usedItems = GetEffectiveUsedItems(bankInfo.FileName);
            var totalCount = bankInfo.TotalCount;
            var usedCount = usedItems.Count;
            var remainingCount = totalCount - usedCount;

            return $"题库：{bankInfo.Name} | 条目总数：{totalCount} | 未抽剩余：{remainingCount} | " +
                   $"本批：{_currentIndex}/{_totalCount} | 对：{_correctAnswers.Count} | 错：{_wrongAnswers.Count}";
        }

        /// <summary>
        /// 重置测验状态
        /// </summary>
        private void ResetQuiz()
        {
            _currentQueue.Clear();
            _correctAnswers.Clear();
            _wrongAnswers.Clear();
            _sessionMarkedItems.Clear();
            _transientUsedItems.Clear();
            _stopwatch?.Stop();
            _stopwatch = null;
            _elapsedMilliseconds = 0;
            _totalCount = 0;
            _currentIndex = 0;
            _currentItem = null;
            _hasAnyAnswer = false;
        }

        /// <summary>
        /// 获取有效的已使用项目集合（包括持久化的和临时的）
        /// </summary>
        private HashSet<string> GetEffectiveUsedItems(string bankFileName)
        {
            var effectiveUsed = new HashSet<string>();

            // 添加持久化的已使用项目
            if (_allUsedItems.TryGetValue(bankFileName, out var persistedItems))
            {
                foreach (var item in persistedItems)
                {
                    effectiveUsed.Add(item);
                }
            }

            // 添加当前会话中的临时项目
            foreach (var item in _transientUsedItems)
            {
                effectiveUsed.Add(item);
            }

            return effectiveUsed;
        }

        /// <summary>
        /// 标记当前项目为已使用
        /// </summary>
        private void MarkCurrentItemAsUsed()
        {
            if (_currentItem == null || _currentBank == null) return;

            _sessionMarkedItems.Add(_currentItem);
            _transientUsedItems.Add(_currentItem);
        }

        /// <summary>
        /// 检查指定题库是否还有可用题目
        /// </summary>
        /// <param name="bankInfo">题库信息</param>
        /// <returns>剩余题目数量</returns>
        public int GetRemainingCount(BankInfo bankInfo)
        {
            var usedItems = GetEffectiveUsedItems(bankInfo.FileName);
            return bankInfo.TotalCount - usedItems.Count;
        }

        /// <summary>
        /// 获取当前题目的详细信息
        /// </summary>
        /// <returns>当前题目详细信息</returns>
        public AcupointInfo? GetCurrentItemInfo()
        {
            if (_currentItem == null || _currentBank == null)
                return null;

            return _currentBank.AcupointDetails.TryGetValue(_currentItem, out var info) ? info : null;
        }
    }
}