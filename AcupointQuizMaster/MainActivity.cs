using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Views;
using AcupointQuizMaster.Models;
using AcupointQuizMaster.Services;

namespace AcupointQuizMaster;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity
{
    // 服务类
    private BankParsingService? _bankParsingService;
    private PersistenceService? _persistenceService;
    private QuizManager? _quizManager;
    private AIService? _aiService;

    // UI控件
    private TextView? _statusText;
    private TextView? _currentItemText;
    private Button? _drawButton;
    private Button? _startButton;
    private Button? _correctButton;
    private Button? _wrongButton;
    private Button? _aiEvaluationButton;
    private Button? _viewDetailButton;
    private Button? _resultButton;
    private Button? _helpButton;
    private Button? _settingsButton;

    // 状态管理
    private QuizState _currentState = QuizState.Preparation;
    private BankInfo? _currentBank;
    private List<BankInfo> _availableBanks = new List<BankInfo>();

    // AI相关状态
    private bool _aiSessionActive = false;
    private Dictionary<string, int> _aiQuestionTypeCounts = new Dictionary<string, int>();

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        try
        {
            SetContentView(Resource.Layout.activity_main);
            InitializeServices();
            InitializeUI();
            LoadAvailableBanks();
            UpdateUIState();
        }
        catch (Exception ex)
        {
            ShowError("初始化失败", ex.Message);
        }
    }

    private void InitializeServices()
    {
        _bankParsingService = new BankParsingService(this);
        _persistenceService = new PersistenceService(this);
        _quizManager = new QuizManager(_persistenceService);
        _aiService = new AIService();
        
        // 加载并应用AI设置
        var appSettings = _persistenceService.LoadSettings();
        _aiService.UpdateSettings(appSettings);
    }

    private void InitializeUI()
    {
        _statusText = FindViewById<TextView>(Resource.Id.statusText);
        _currentItemText = FindViewById<TextView>(Resource.Id.currentItemText);
        _drawButton = FindViewById<Button>(Resource.Id.drawButton);
        _startButton = FindViewById<Button>(Resource.Id.startButton);
        _correctButton = FindViewById<Button>(Resource.Id.correctButton);
        _wrongButton = FindViewById<Button>(Resource.Id.wrongButton);
        _aiEvaluationButton = FindViewById<Button>(Resource.Id.aiEvaluationButton);
        _viewDetailButton = FindViewById<Button>(Resource.Id.viewDetailButton);
        _resultButton = FindViewById<Button>(Resource.Id.resultButton);
        _helpButton = FindViewById<Button>(Resource.Id.helpButton);
        _settingsButton = FindViewById<Button>(Resource.Id.settingsButton);

        // 绑定事件
        if (_drawButton != null) _drawButton.Click += OnDrawClick;
        if (_startButton != null) _startButton.Click += OnStartClick;
        if (_correctButton != null) _correctButton.Click += OnCorrectClick;
        if (_wrongButton != null) _wrongButton.Click += OnWrongClick;
        if (_aiEvaluationButton != null) _aiEvaluationButton.Click += OnAIEvaluationClick;
        if (_viewDetailButton != null) _viewDetailButton.Click += OnViewDetailClick;
        if (_resultButton != null) _resultButton.Click += OnResultClick;
        if (_helpButton != null) _helpButton.Click += OnHelpClick;
        if (_settingsButton != null) _settingsButton.Click += OnSettingsClick;
    }

    private void LoadAvailableBanks()
    {
        try
        {
            if (_bankParsingService == null) return;

            var bankFiles = _bankParsingService.GetAvailableBankFiles();
            _availableBanks.Clear();

            foreach (var fileName in bankFiles)
            {
                try
                {
                    var bankInfo = _bankParsingService.ParseBank(fileName);
                    if (bankInfo.TotalCount > 0)
                    {
                        _availableBanks.Add(bankInfo);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"解析题库 {fileName} 失败: {ex.Message}");
                }
            }

            if (_availableBanks.Count == 0)
            {
                ShowError("未找到题库", "请确保在 assets 目录中有有效的题库文件。");
            }
        }
        catch (Exception ex)
        {
            ShowError("加载题库失败", ex.Message);
        }
    }

    private async void OnDrawClick(object? sender, EventArgs e)
    {
        try
        {
            if (_availableBanks.Count == 0)
            {
                ShowError("未找到题库", "请确保有有效的题库文件。");
                return;
            }

            var selectedBank = await ShowBankSelectionDialog();
            if (selectedBank == null) return;

            _currentBank = selectedBank;
            _quizManager?.SetCurrentBank(selectedBank);

            var remainingCount = _quizManager?.GetRemainingCount(selectedBank) ?? 0;
            if (remainingCount == 0)
            {
                var shouldClear = await ShowYesNoDialog("该经络已抽完", $"经络【{selectedBank.Name}】已经全部答完，是否清除标记重新开始？");
                if (shouldClear)
                {
                    // 清除该经络的标记
                    _quizManager?.ClearUsedItems(selectedBank.FileName);
                    remainingCount = selectedBank.TotalCount; // 重新计算剩余数量
                }
                else
                {
                    return; // 用户选择不清除，直接返回
                }
            }

            var quantity = await ShowQuantityInputDialog(remainingCount);
            if (quantity <= 0) return;

            var success = _quizManager?.StartQuiz(quantity) ?? false;
            if (success)
            {
                _currentState = QuizState.Ready;
                UpdateUIState();
                UpdateStatus();
            }
            else
            {
                ShowError("开始失败", "无法开始测验，请重试。");
            }
        }
        catch (Exception ex)
        {
            ShowError("抽题失败", ex.Message);
        }
    }

    private void OnStartClick(object? sender, EventArgs e)
    {
        try
        {
            if (_quizManager == null) return;

            var nextItem = _quizManager.GetNextItem();
            if (nextItem != null)
            {
                _currentState = QuizState.Quiz;
                _currentItemText?.SetText(nextItem, TextView.BufferType.Normal);
                UpdateUIState();
                UpdateStatus();
            }
        }
        catch (Exception ex)
        {
            ShowError("开始测验失败", ex.Message);
        }
    }

    private async void OnCorrectClick(object? sender, EventArgs e)
    {
        try
        {
            if (_quizManager == null) return;

            _quizManager.MarkCorrect();
            // 自动保存进度
            Task.Run(() => _quizManager?.SaveProgress());
            await ProcessNextQuestion();
        }
        catch (Exception ex)
        {
            ShowError("标记正确失败", ex.Message);
        }
    }

    private async void OnWrongClick(object? sender, EventArgs e)
    {
        try
        {
            if (_quizManager == null) return;

            var currentItem = _quizManager.CurrentItem;
            var currentInfo = _quizManager.GetCurrentItemInfo();

            _quizManager.MarkWrong();
            // 自动保存进度
            Task.Run(() => _quizManager?.SaveProgress());

            if (currentItem != null && currentInfo != null)
            {
                await ShowWrongAnswerDialog(currentItem, currentInfo);
            }

            await ProcessNextQuestion();
        }
        catch (Exception ex)
        {
            ShowError("标记错误失败", ex.Message);
        }
    }

    private async void OnAIEvaluationClick(object? sender, EventArgs e)
    {
        try
        {
            if (_currentState == QuizState.Quiz)
            {
                ShowMessage("测验进行中", "当前存在未完成的抽题批次，AI测评暂不可用。请先完成本批次。");
                return;
            }

            // 切换到AI测评界面
            _currentState = QuizState.AIEvaluation;
            UpdateUIState();

            var selectedBank = await ShowBankSelectionDialog();
            if (selectedBank == null) 
            {
                // 用户取消，返回主界面
                _currentState = QuizState.Preparation;
                UpdateUIState();
                return;
            }

            _aiSessionActive = true;
            _aiQuestionTypeCounts.Clear();

            await RunAIEvaluationRound(selectedBank);
        }
        catch (Exception ex)
        {
            RestoreButtonsAfterAI();
            ShowError("AI测评失败", ex.Message);
        }
    }

    private async Task RunAIEvaluationRound(BankInfo bankInfo)
    {
        try
        {
            // 选择穴位
            var acupoint = SelectRandomAcupoint(bankInfo);
            if (acupoint == null)
            {
                ShowError("没有可用穴位", "该题库没有可用条目。");
                RestoreButtonsAfterAI();
                return;
            }

            // AI出题
            Title = "AI正在出题，请稍后";
            var progressDialog = ShowProgressDialog("正在由AI出题... 请稍候");

            var (question, canonicalAnswer, questionType) = await _aiService!.BuildQuestionForcedAsync(
                acupoint.Name, acupoint, SelectBalancedQuestionType(acupoint));

            progressDialog?.Dismiss();

            // 更新题型计数
            _aiQuestionTypeCounts[questionType] = (_aiQuestionTypeCounts.ContainsKey(questionType) ? _aiQuestionTypeCounts[questionType] : 0) + 1;

            // 答题
            var userAnswer = await ShowAnswerInputDialog(question);
            if (string.IsNullOrEmpty(userAnswer))
            {
                RestoreButtonsAfterAI();
                return;
            }

            // AI判卷
            Title = "AI正在判卷，请稍后";
            progressDialog = ShowProgressDialog("正在由AI判卷... 请稍候");

            var gradeResult = await _aiService.GradeAnswerAsync(
                question, userAnswer, acupoint.Name, questionType, acupoint);

            progressDialog?.Dismiss();

            // 显示结果
            var shouldContinue = await ShowAIResultDialog(acupoint.Name, questionType, question, gradeResult);
            
            if (shouldContinue)
            {
                await RunAIEvaluationRound(bankInfo);
            }
            else
            {
                RestoreButtonsAfterAI();
            }
        }
        catch (Exception ex)
        {
            ShowError("AI测评失败", ex.Message);
            RestoreButtonsAfterAI();
        }
    }

    private void OnViewDetailClick(object? sender, EventArgs e)
    {
        try
        {
            var currentInfo = _quizManager?.GetCurrentItemInfo();
            var currentItem = _quizManager?.CurrentItem;

            if (currentItem != null && currentInfo != null)
            {
                ShowAcupointDetailDialog(currentItem, currentInfo);
            }
        }
        catch (Exception ex)
        {
            ShowError("查看详情失败", ex.Message);
        }
    }

    private void OnResultClick(object? sender, EventArgs e)
    {
        try
        {
            if (_quizManager == null) return;

            var result = _quizManager.GetQuizResult();
            var wrongDetails = _quizManager.GetWrongItemsDetails();

            ShowQuizResultDialog(result, wrongDetails);
        }
        catch (Exception ex)
        {
            ShowError("查看结果失败", ex.Message);
        }
    }

    private void OnHelpClick(object? sender, EventArgs e)
    {
        ShowHelpDialog();
    }
    private void OnSettingsClick(object? sender, EventArgs e)
    {
        var intent = new Intent(this, typeof(SettingsActivity));        StartActivity(intent);
    }

    private async Task ProcessNextQuestion()
    {
        if (_quizManager == null) return;

        await Task.Delay(500); // 短暂延迟

        var nextItem = _quizManager.GetNextItem();
        if (nextItem != null)
        {
            _currentItemText?.SetText(nextItem, TextView.BufferType.Normal);
            UpdateStatus();
        }
        else
        {
            // 测验结束
            _quizManager.FinishQuiz();
            _currentState = QuizState.Finished;
            _currentItemText?.SetText("尚未开始", TextView.BufferType.Normal);
            UpdateUIState();
            UpdateStatus();
            Title = "穴位测验大师";
        }
    }

    private void UpdateUIState()
    {
        RunOnUiThread(() =>
        {
            var currentItemLayout = FindViewById<LinearLayout>(Resource.Id.currentItemLayout);
            var quizButtonsLayout = FindViewById<LinearLayout>(Resource.Id.quizButtonsLayout);
            var detailButtonsLayout = FindViewById<LinearLayout>(Resource.Id.detailButtonsLayout);
            
            // 获取主界面按钮容器
            var mainButtonsLayout = FindViewById<LinearLayout>(Resource.Id.mainButtonsLayout);

            switch (_currentState)
            {
                case QuizState.Preparation:
                    // 主界面：只显示3个主要按钮
                    if (mainButtonsLayout != null) mainButtonsLayout.Visibility = ViewStates.Visible;
                    if (_drawButton != null) _drawButton.Enabled = true;
                    if (_aiEvaluationButton != null) _aiEvaluationButton.Enabled = true;
                    if (_helpButton != null) _helpButton.Enabled = true;
                    if (_settingsButton != null) _settingsButton.Enabled = true;
                    
                    // 隐藏抽穴位相关界面
                    if (currentItemLayout != null) currentItemLayout.Visibility = ViewStates.Gone;
                    if (quizButtonsLayout != null) quizButtonsLayout.Visibility = ViewStates.Gone;
                    if (detailButtonsLayout != null) detailButtonsLayout.Visibility = ViewStates.Gone;
                    if (_statusText != null) _statusText.SetText("欢迎使用穴位测验大师", TextView.BufferType.Normal);
                    Title = "穴位测验大师";
                    break;

                case QuizState.Ready:
                    // 抽穴位界面：隐藏主界面按钮，显示抽穴位相关界面
                    if (mainButtonsLayout != null) mainButtonsLayout.Visibility = ViewStates.Gone;
                    if (currentItemLayout != null) currentItemLayout.Visibility = ViewStates.Visible;
                    if (quizButtonsLayout != null) quizButtonsLayout.Visibility = ViewStates.Visible;
                    if (detailButtonsLayout != null) detailButtonsLayout.Visibility = ViewStates.Visible;
                    
                    // 抽穴位按钮状态
                    if (_startButton != null) _startButton.Enabled = true;
                    if (_correctButton != null) _correctButton.Enabled = false;
                    if (_wrongButton != null) _wrongButton.Enabled = false;
                    if (_viewDetailButton != null) _viewDetailButton.Enabled = false;
                    if (_resultButton != null) _resultButton.Enabled = false;
                    if (_resultButton != null) _resultButton.Visibility = ViewStates.Gone;
                    Title = "抽穴位练习";
                    break;

                case QuizState.Quiz:
                    // 抽穴位测试中：隐藏主界面按钮
                    if (mainButtonsLayout != null) mainButtonsLayout.Visibility = ViewStates.Gone;
                    if (currentItemLayout != null) currentItemLayout.Visibility = ViewStates.Visible;
                    if (quizButtonsLayout != null) quizButtonsLayout.Visibility = ViewStates.Visible;
                    if (detailButtonsLayout != null) detailButtonsLayout.Visibility = ViewStates.Visible;
                    
                    // 抽穴位按钮状态
                    if (_startButton != null) _startButton.Enabled = false;
                    if (_correctButton != null) _correctButton.Enabled = true;
                    if (_wrongButton != null) _wrongButton.Enabled = true;
                    if (_viewDetailButton != null) _viewDetailButton.Enabled = true;
                    if (_resultButton != null) _resultButton.Enabled = false;
                    if (_resultButton != null) _resultButton.Visibility = ViewStates.Gone;
                    Title = "抽穴位练习中...";
                    break;

                case QuizState.Finished:
                    // 抽穴位完成：隐藏主界面按钮，显示结果
                    if (mainButtonsLayout != null) mainButtonsLayout.Visibility = ViewStates.Gone;
                    if (currentItemLayout != null) currentItemLayout.Visibility = ViewStates.Visible;
                    if (quizButtonsLayout != null) quizButtonsLayout.Visibility = ViewStates.Visible;
                    if (detailButtonsLayout != null) detailButtonsLayout.Visibility = ViewStates.Visible;
                    
                    // 抽穴位按钮状态
                    if (_startButton != null) _startButton.Enabled = false;
                    if (_correctButton != null) _correctButton.Enabled = false;
                    if (_wrongButton != null) _wrongButton.Enabled = false;
                    if (_viewDetailButton != null) _viewDetailButton.Enabled = false;
                    if (_resultButton != null) _resultButton.Enabled = true;
                    if (_resultButton != null) _resultButton.Visibility = ViewStates.Visible;
                    Title = "抽穴位练习完成";
                    break;

                case QuizState.AIEvaluation:
                    // AI穴位测评界面：隐藏主界面按钮和抽穴位界面
                    if (mainButtonsLayout != null) mainButtonsLayout.Visibility = ViewStates.Gone;
                    if (currentItemLayout != null) currentItemLayout.Visibility = ViewStates.Gone;
                    if (quizButtonsLayout != null) quizButtonsLayout.Visibility = ViewStates.Gone;
                    if (detailButtonsLayout != null) detailButtonsLayout.Visibility = ViewStates.Gone;
                    if (_statusText != null) _statusText.SetText("AI穴位测评进行中...", TextView.BufferType.Normal);
                    Title = "AI穴位测评";
                    break;
            }

            // AI会话期间的按钮状态
            if (_aiSessionActive)
            {
                if (_aiEvaluationButton != null) _aiEvaluationButton.Enabled = false;
                if (_drawButton != null) _drawButton.Enabled = false;
                if (_helpButton != null) _helpButton.Enabled = false;
                if (_settingsButton != null) _settingsButton.Enabled = false;
            }
            else
            {
                if (_aiEvaluationButton != null && _currentState != QuizState.Quiz) 
                    _aiEvaluationButton.Enabled = true;
            }
        });
    }

    private void UpdateStatus()
    {
        if (_quizManager != null && _currentBank != null)
        {
            var statusText = _quizManager.GetBankStats(_currentBank);
            _statusText?.SetText(statusText, TextView.BufferType.Normal);
        }
        else
        {
            _statusText?.SetText("未选择题库。", TextView.BufferType.Normal);
        }
    }

    // 对话框方法
    private Task<BankInfo?> ShowBankSelectionDialog()
    {
        var tcs = new TaskCompletionSource<BankInfo?>();
        
        var bankNames = _availableBanks.Select(b => b.Name).ToArray();
        
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle("选择题库");
        builder.SetItems(bankNames, (sender, e) =>
        {
            if (e.Which >= 0 && e.Which < _availableBanks.Count)
            {
                tcs.SetResult(_availableBanks[e.Which]);
            }
            else
            {
                tcs.SetResult(null);
            }
        });
        builder.SetNegativeButton("取消", (sender, e) => tcs.SetResult(null));
        
        builder.Show();
        return tcs.Task;
    }

    private Task<int> ShowQuantityInputDialog(int maxQuantity)
    {
        var tcs = new TaskCompletionSource<int>();
        
        var input = new EditText(this);
        input.InputType = Android.Text.InputTypes.ClassNumber;
        input.Hint = $"请输入要抽取的穴位数量（剩余可抽：{maxQuantity}）";
        input.Text = maxQuantity.ToString(); // 默认填入最大可抽数量
        input.SelectAll(); // 选中所有文本，方便用户修改
        
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle("抽取数量");
        builder.SetView(input);
        builder.SetPositiveButton("确定", (sender, e) =>
        {
            if (int.TryParse(input.Text, out var quantity) && quantity > 0)
            {
                tcs.SetResult(Math.Min(quantity, maxQuantity));
            }
            else
            {
                ShowMessage("输入错误", "请输入有效的正整数。");
                tcs.SetResult(0);
            }
        });
        builder.SetNegativeButton("取消", (sender, e) => tcs.SetResult(0));
        
        builder.Show();
        return tcs.Task;
    }

    private Task<string> ShowAnswerInputDialog(string question)
    {
        var tcs = new TaskCompletionSource<string>();
        
        var input = new EditText(this);
        input.SetLines(3);
        input.Hint = "请输入答案";
        
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle("AI测评");
        builder.SetMessage(question);
        builder.SetView(input);
        builder.SetPositiveButton("确定", (sender, e) => tcs.SetResult(input.Text ?? ""));
        builder.SetNegativeButton("取消", (sender, e) => tcs.SetResult(""));
        builder.SetCancelable(false); // 防止ESC关闭
        
        builder.Show();
        return tcs.Task;
    }

    private Task<bool> ShowAIResultDialog(string acupointName, string questionType, string question, AIGradeResponse gradeResult)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        var passStr = gradeResult.Pass ? "通过 ✅" : "未通过 ❌";
        var summary = $"穴位：{acupointName}\n" +
                     $"维度：{questionType}\n" +
                     $"题目：{question}\n" +
                     $"得分：{gradeResult.Score:F1}\n" +
                     $"判定：{passStr}\n\n" +
                     $"参考答案：\n{gradeResult.ModelAnswer}\n\n" +
                     $"评语：\n{gradeResult.Feedback}";

        if (!gradeResult.Pass && !string.IsNullOrEmpty(gradeResult.IncorrectReason))
        {
            summary += $"\n\n错误原因：\n{gradeResult.IncorrectReason}";
        }

        var builder = new AlertDialog.Builder(this);
        builder.SetTitle("测评结果");
        builder.SetMessage(summary);
        builder.SetPositiveButton("继续测评", (sender, e) => tcs.SetResult(true));
        builder.SetNegativeButton("关闭", (sender, e) => tcs.SetResult(false));
        
        builder.Show();
        return tcs.Task;
    }

    private Task ShowWrongAnswerDialog(string acupointName, AcupointInfo acupointInfo)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        var content = FormatAcupointDetails(acupointName, acupointInfo, true);
        
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle($"答错：{acupointName}");
        builder.SetMessage(content);
        builder.SetPositiveButton("下一个", (sender, e) => tcs.SetResult(true));
        
        builder.Show();
        return tcs.Task;
    }

    private void ShowAcupointDetailDialog(string acupointName, AcupointInfo acupointInfo)
    {
        var content = FormatAcupointDetails(acupointName, acupointInfo, false);
        
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle($"查看穴位：{acupointName}");
        builder.SetMessage(content);
        builder.SetPositiveButton("关闭", (sender, e) => { });
        
        builder.Show();
    }

    private void ShowQuizResultDialog(QuizResult result, List<(string Name, AcupointInfo Info)> wrongDetails)
    {
        var summary = $"题库：{result.BankName}\n" +
                     $"本批总数：{result.TotalCount}\n" +
                     $"答对数：{result.CorrectCount}\n" +
                     $"答错数：{result.WrongCount}\n" +
                     $"正确率：{result.FormattedAccuracy}\n" +
                     $"用时：{result.FormattedTime}\n\n";

        if (wrongDetails.Count > 0)
        {
            summary += "【答错明细】\n";
            for (int i = 0; i < wrongDetails.Count; i++)
            {
                var (name, info) = wrongDetails[i];
                summary += $"{i + 1}. {FormatAcupointDetails(name, info, false)}\n\n";
            }
        }
        else
        {
            summary += "（无答错项）";
        }

        var builder = new AlertDialog.Builder(this);
        builder.SetTitle("测验结果");
        builder.SetMessage(summary);
        builder.SetPositiveButton("关闭", (sender, e) => { });
        
        builder.Show();
    }

    private void ShowHelpDialog()
    {
        var helpText = """
            《穴位测验大师》使用指南

            【这是什么】
            用于抽题练习穴位的小工具。先选题库，设抽取数量，开始答题：对按"答对"，错按"答错"。
            一批做完，会给出统计与错题列表。

            【快速开始】
            1) 点击"抽穴位"选择题库，输入要抽取的数量。
            2) 点击"开始抽取"开始练习：
               - 看到"当前穴位"后，答对点"答对"，答错点"答错"。
               - 可以点击"查看穴位信息"查看详情。
            3) 一批结束后，点击"查看结果"查看统计与错题明细。

            【AI测评功能】
            - 点击"穴位测评"可使用AI出题和判卷功能
            - AI会根据穴位信息自动出题并评分
            - 支持多种题型的智能平衡出题

            【其他功能】
            - 程序会自动记录已答过的穴位
            - 再次选择相同题库时会排除已答穴位
            - 可在选择题库时清空记录重新开始
            """;

        var builder = new AlertDialog.Builder(this);
        builder.SetTitle("帮助");
        builder.SetMessage(helpText);
        builder.SetPositiveButton("关闭", (sender, e) => { });
        
        builder.Show();
    }

    // 辅助方法
    private string FormatAcupointDetails(string name, AcupointInfo info, bool showMeridian)
    {
        var parts = new List<string> { name, "" };
        
        if (showMeridian && !string.IsNullOrEmpty(info.Meridian))
        {
            parts.Add($"归经：{info.Meridian}");
        }
        
        parts.Add($"定位：{info.Location ?? "（无）"}");
        
        var treatment = info.Treatment ?? "（无）";
        if (treatment.StartsWith("病症：") || treatment.StartsWith("病证："))
        {
            parts.Add($"主治 {treatment}");
        }
        else
        {
            parts.Add($"主治：{treatment}");
        }
        
        if (!string.IsNullOrEmpty(info.SpecialType))
        {
            parts.Add($"特定穴：{info.SpecialType}");
        }
        
        return string.Join("\n", parts);
    }

    private AcupointInfo? SelectRandomAcupoint(BankInfo bankInfo)
    {
        var availableNames = bankInfo.AcupointNames.Where(name => 
            bankInfo.AcupointDetails.ContainsKey(name) && 
            bankInfo.AcupointDetails[name].GetAllFields().Count > 0).ToList();
        
        if (availableNames.Count == 0) return null;
        
        var random = new Random();
        var selectedName = availableNames[random.Next(availableNames.Count)];
        
        return bankInfo.AcupointDetails[selectedName];
    }

    private string SelectBalancedQuestionType(AcupointInfo acupoint)
    {
        var fields = acupoint.GetAllFields();
        var fieldNames = fields.Keys.ToList();
        
        if (fieldNames.Count == 0) return "定位";
        
        // 找出出现次数最少的题型
        var minCount = fieldNames.Min(field => _aiQuestionTypeCounts.ContainsKey(field) ? _aiQuestionTypeCounts[field] : 0);
        var candidateFields = fieldNames.Where(field => 
            (_aiQuestionTypeCounts.ContainsKey(field) ? _aiQuestionTypeCounts[field] : 0) == minCount).ToList();
        
        var random = new Random();
        return candidateFields[random.Next(candidateFields.Count)];
    }

    private void RestoreButtonsAfterAI()
    {
        _aiSessionActive = false;
        
        // 返回到主界面
        _currentState = QuizState.Preparation;
        UpdateUIState();
    }

    private ProgressDialog? ShowProgressDialog(string message)
    {
        var dialog = new ProgressDialog(this);
        dialog.SetMessage(message);
        dialog.SetCancelable(false);
        dialog.Show();
        return dialog;
    }

    private void ShowError(string title, string message)
    {
        RunOnUiThread(() =>
        {
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle(title);
            builder.SetMessage(message);
            builder.SetPositiveButton("确定", (sender, e) => { });
            builder.Show();
        });
    }

    private void ShowMessage(string title, string message)
    {
        RunOnUiThread(() =>
        {
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle(title);
            builder.SetMessage(message);
            builder.SetPositiveButton("确定", (sender, e) => { });
            builder.Show();
        });
    }

    protected override void OnPause()
    {
        base.OnPause();
        
        // 保存进度
        if (_quizManager?.HasAnyAnswer == true)
        {
            Task.Run(() => _quizManager?.SaveProgress());
        }
    }


    public override void OnBackPressed()
    {
        // 处理返回按钮逻辑
        switch (_currentState)
        {
            case QuizState.Preparation:
                // 主界面时，正常退出应用
                base.OnBackPressed();
                break;
            
            case QuizState.Ready:
            case QuizState.Finished:
                // 抽穴位准备/完成状态，返回主界面
                _currentState = QuizState.Preparation;
                _currentBank = null;
                UpdateUIState();
                break;

            case QuizState.Quiz:
                // 抽穴位进行中，询问是否保存已标记的条目
                ShowSaveProgressDialog();
                return;
            
            case QuizState.AIEvaluation:
                // AI测评状态，返回主界面
                RestoreButtonsAfterAI();
                break;
            
            default:
                base.OnBackPressed();
                break;
        }
    }

    private async void ShowSaveProgressDialog()
    {
        var hasAnswers = _quizManager?.HasAnyAnswer ?? false;
        
        if (hasAnswers)
        {
            var result = await ShowYesNoDialog("保存进度", "是否保存本次已标记的条目？");
            if (result)
            {
                _quizManager?.SaveProgress();
            }
        }
        
        // 返回主界面
        _currentState = QuizState.Preparation;
        _currentBank = null;
        UpdateUIState();
    }

    private Task<bool> ShowYesNoDialog(string title, string message)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(title);
        builder.SetMessage(message);
        builder.SetPositiveButton("是", (sender, e) => tcs.SetResult(true));
        builder.SetNegativeButton("否", (sender, e) => tcs.SetResult(false));
        builder.SetCancelable(false);
        
        builder.Show();
        return tcs.Task;
    }

    protected override void OnDestroy()
    {
        _aiService?.Dispose();
        base.OnDestroy();
    }
}

public enum QuizState
{
    Preparation,    // 主界面，显示3个主要按钮
    Ready,          // 抽穴位准备状态
    Quiz,           // 抽穴位进行中
    Finished,       // 抽穴位完成
    AIEvaluation    // AI穴位测评界面
}
