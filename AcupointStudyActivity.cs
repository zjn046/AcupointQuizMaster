using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Views;
using AcupointQuizMaster.Models;
using AcupointQuizMaster.Services;
using SystemException = System.Exception;

namespace AcupointQuizMaster
{
    [Activity(Label = "穴位学习")]
    public class AcupointStudyActivity : Activity
    {
        private BankParsingService? _bankParsingService;
        private List<BankInfo> _availableBanks = new List<BankInfo>();
        
        // UI控件
        private TextView? _titleText;
        private SeekBar? _meridianSeekBar;
        private TextView? _meridianLabel;
        private SeekBar? _acupointSeekBar;
        private TextView? _acupointLabel;
        private TextView? _acupointDetailText;
        private Button? _backButton;
        
        private BankInfo? _selectedBank;
        private readonly List<string> _meridianNames = new List<string>();
        private readonly List<string> _acupointNames = new List<string>();
        

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            try
            {
                SetContentView(Resource.Layout.activity_acupoint_study);
                InitializeServices();
                InitializeUI();
                LoadAvailableBanks();
            }
            catch (SystemException ex)
            {
                ShowError("初始化失败", ex.Message);
                Finish();
            }
        }

        private void InitializeServices()
        {
            _bankParsingService = new BankParsingService(this);
        }


        private void InitializeUI()
        {
            _titleText = FindViewById<TextView>(Resource.Id.titleText);
            _meridianSeekBar = FindViewById<SeekBar>(Resource.Id.meridianSeekBar);
            _meridianLabel = FindViewById<TextView>(Resource.Id.meridianLabel);
            _acupointSeekBar = FindViewById<SeekBar>(Resource.Id.acupointSeekBar);
            _acupointLabel = FindViewById<TextView>(Resource.Id.acupointLabel);
            _acupointDetailText = FindViewById<TextView>(Resource.Id.acupointDetailText);
            _backButton = FindViewById<Button>(Resource.Id.backButton);

            if (_meridianSeekBar != null) 
            {
                _meridianSeekBar.ProgressChanged += OnMeridianSeekBarChanged;
            }
            
            if (_acupointSeekBar != null) 
            {
                _acupointSeekBar.ProgressChanged += OnAcupointSeekBarChanged;
            }
            
            // 添加点击事件处理
            if (_meridianLabel != null) 
            {
                _meridianLabel.Click += OnMeridianLabelClick;
                // 设置无障碍代理
                var meridianDelegate = new AdjustableViewAccessibilityDelegate(
                    _meridianLabel,
                    () => IncrementMeridian(),
                    () => DecrementMeridian(),
                    () => _meridianLabel.Text ?? "",
                    "经络选择"
                );
                _meridianLabel.SetAccessibilityDelegate(meridianDelegate);
            }
            
            if (_acupointLabel != null) 
            {
                _acupointLabel.Click += OnAcupointLabelClick;
                // 设置无障碍代理
                var acupointDelegate = new AdjustableViewAccessibilityDelegate(
                    _acupointLabel,
                    () => IncrementAcupoint(),
                    () => DecrementAcupoint(),
                    () => _acupointLabel.Text ?? "",
                    "穴位选择"
                );
                _acupointLabel.SetAccessibilityDelegate(acupointDelegate);
            }
            
            if (_backButton != null) _backButton.Click += OnBackClick;
        }

        private void LoadAvailableBanks()
        {
            try
            {
                if (_bankParsingService == null) return;

                var bankFiles = _bankParsingService.GetAvailableBankFiles();
                _availableBanks.Clear();
                _meridianNames.Clear();

                foreach (var fileName in bankFiles)
                {
                    try
                    {
                        var bankInfo = _bankParsingService.ParseBank(fileName);
                        if (bankInfo.TotalCount > 0)
                        {
                            _availableBanks.Add(bankInfo);
                            _meridianNames.Add(bankInfo.Name);
                        }
                    }
                    catch (SystemException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"解析题库 {fileName} 失败: {ex.Message}");
                    }
                }

                if (_availableBanks.Count == 0)
                {
                    ShowError("未找到题库", "请确保在 assets 目录中有有效的题库文件。");
                    return;
                }

                // 设置经络选择器
                SetupMeridianSeekBar();
                
            }
            catch (SystemException ex)
            {
                ShowError("加载题库失败", ex.Message);
            }
        }

        private void SetupMeridianSeekBar()
        {
            if (_meridianSeekBar == null || _meridianLabel == null) return;

            if (_meridianNames.Count == 0)
            {
                _meridianSeekBar.Max = 0;
                _meridianSeekBar.Progress = 0;
                _meridianSeekBar.Enabled = false;
                _meridianLabel.Text = "(无题库)";
                _meridianLabel.ContentDescription = "经络选择，暂无可用题库";
                return;
            }

            _meridianSeekBar.Max = _meridianNames.Count - 1;
            _meridianSeekBar.Progress = 0;
            _meridianSeekBar.Enabled = true;
            
            _meridianLabel.Text = _meridianNames[0];
            _meridianLabel.ContentDescription = $"经络选择，当前选择: {_meridianNames[0]}，共{_meridianNames.Count}个选项";
            
            // 自动触发第一个经络的选择，这样会自动加载第一个穴位并显示详情
            _selectedBank = _availableBanks[0];
            UpdateAcupointSeekBar();
            
            // 如果有穴位，自动显示第一个穴位的详情
            if (_acupointNames.Count > 0 && _selectedBank != null)
            {
                var firstAcupointName = _acupointNames[0];
                if (_selectedBank.AcupointDetails.TryGetValue(firstAcupointName, out var acupointInfo))
                {
                    ShowAcupointDetails(firstAcupointName, acupointInfo);
                }
            }
        }

        private void OnMeridianSeekBarChanged(object? sender, SeekBar.ProgressChangedEventArgs e)
        {
            try
            {
                if (!e.FromUser || e.Progress < 0 || e.Progress >= _availableBanks.Count) return;

                _selectedBank = _availableBanks[e.Progress];
                
                if (_meridianLabel != null)
                {
                    _meridianLabel.Text = _meridianNames[e.Progress];
                    _meridianLabel.ContentDescription = $"经络选择，当前选择: {_meridianNames[e.Progress]}，第{e.Progress + 1}个，共{_meridianNames.Count}个选项";
                }
                
                // 更新穴位选择器
                UpdateAcupointSeekBar();
                
                // 清空详情显示
                _acupointDetailText?.SetText("请选择要学习的穴位", TextView.BufferType.Normal);
            }
            catch (SystemException ex)
            {
                ShowError("经络选择失败", ex.Message);
            }
        }

        private void UpdateAcupointSeekBar()
        {
            if (_acupointSeekBar == null || _acupointLabel == null || _selectedBank == null) return;

            _acupointNames.Clear();
            _acupointNames.AddRange(_selectedBank.AcupointNames);

            if (_acupointNames.Count == 0)
            {
                _acupointSeekBar.Max = 0;
                _acupointSeekBar.Progress = 0;
                _acupointSeekBar.Enabled = false;
                _acupointLabel.Text = "(该经络无穴位)";
                _acupointLabel.ContentDescription = "穴位选择，该经络暂无穴位信息";
                return;
            }

            _acupointSeekBar.Max = _acupointNames.Count - 1;
            _acupointSeekBar.Progress = 0;
            _acupointSeekBar.Enabled = true;
            
            _acupointLabel.Text = _acupointNames[0];
            _acupointLabel.ContentDescription = $"穴位选择，当前选择: {_acupointNames[0]}，共{_acupointNames.Count}个选项";
        }

        private void OnAcupointSeekBarChanged(object? sender, SeekBar.ProgressChangedEventArgs e)
        {
            try
            {
                if (!e.FromUser || e.Progress < 0 || e.Progress >= _acupointNames.Count || _selectedBank == null) return;

                var selectedAcupointName = _acupointNames[e.Progress];
                
                if (_acupointLabel != null)
                {
                    _acupointLabel.Text = selectedAcupointName;
                    _acupointLabel.ContentDescription = $"穴位选择，当前选择: {selectedAcupointName}，第{e.Progress + 1}个，共{_acupointNames.Count}个选项";
                }
                
                if (_selectedBank.AcupointDetails.TryGetValue(selectedAcupointName, out var acupointInfo))
                {
                    // 显示穴位详细信息
                    ShowAcupointDetails(selectedAcupointName, acupointInfo);
                }
            }
            catch (SystemException ex)
            {
                ShowError("穴位选择失败", ex.Message);
            }
        }

        private void ShowAcupointDetails(string acupointName, AcupointInfo acupointInfo)
        {
            if (_acupointDetailText == null) return;

            var details = FormatAcupointDetails(acupointName, acupointInfo);
            _acupointDetailText.SetText(details, TextView.BufferType.Normal);
        }

        private string FormatAcupointDetails(string name, AcupointInfo info)
        {
            var parts = new List<string>
            {
                $"【穴位名称】{name}",
                ""
            };

            if (!string.IsNullOrEmpty(info.Meridian))
            {
                parts.Add($"【归经】{info.Meridian}");
                parts.Add("");
            }

            parts.Add($"【定位】{info.Location ?? "（无相关信息）"}");
            parts.Add("");

            var treatment = info.Treatment ?? "（无相关信息）";
            if (treatment.StartsWith("病症：") || treatment.StartsWith("病证："))
            {
                parts.Add($"【主治】{treatment}");
            }
            else
            {
                parts.Add($"【主治】{treatment}");
            }

            if (!string.IsNullOrEmpty(info.SpecialType))
            {
                parts.Add("");
                parts.Add($"【特定穴】{info.SpecialType}");
            }

            return string.Join("\n", parts);
        }

        private void OnBackClick(object? sender, EventArgs e)
        {
            Finish();
        }

        // 经络点击弹出选择对话框
        private void OnMeridianLabelClick(object? sender, EventArgs e)
        {
            try
            {
                if (_meridianNames.Count == 0) return;

                ShowMeridianSelectionDialog();
            }
            catch (SystemException ex)
            {
                ShowError("选择经络失败", ex.Message);
            }
        }

        // 穴位点击弹出选择对话框
        private void OnAcupointLabelClick(object? sender, EventArgs e)
        {
            try
            {
                if (_acupointNames.Count == 0) return;

                ShowAcupointSelectionDialog();
            }
            catch (SystemException ex)
            {
                ShowError("选择穴位失败", ex.Message);
            }
        }

        // 经络选择对话框
        private void ShowMeridianSelectionDialog()
        {
            if (_meridianNames.Count == 0) return;

            var builder = new AlertDialog.Builder(this);
            builder.SetTitle("选择经络");
            
            var currentIndex = _meridianSeekBar?.Progress ?? 0;
            
            builder.SetSingleChoiceItems(_meridianNames.ToArray(), currentIndex, (sender, e) =>
            {
                // 更新SeekBar进度
                if (_meridianSeekBar != null)
                {
                    _meridianSeekBar.Progress = e.Which;
                }
                
                // 触发SeekBar的ProgressChanged事件来更新UI
                OnMeridianSeekBarChanged(_meridianSeekBar, new SeekBar.ProgressChangedEventArgs(_meridianSeekBar, e.Which, true));
                
                // 关闭对话框
                ((AlertDialog)sender!).Dismiss();
            });
            
            builder.SetNegativeButton("取消", (sender, e) => { });
            builder.Show();
        }

        // 穴位选择对话框
        private void ShowAcupointSelectionDialog()
        {
            if (_acupointNames.Count == 0) return;

            var builder = new AlertDialog.Builder(this);
            builder.SetTitle("选择穴位");
            
            var currentIndex = _acupointSeekBar?.Progress ?? 0;
            
            builder.SetSingleChoiceItems(_acupointNames.ToArray(), currentIndex, (sender, e) =>
            {
                // 更新SeekBar进度
                if (_acupointSeekBar != null)
                {
                    _acupointSeekBar.Progress = e.Which;
                }
                
                // 触发SeekBar的ProgressChanged事件来更新UI
                OnAcupointSeekBarChanged(_acupointSeekBar, new SeekBar.ProgressChangedEventArgs(_acupointSeekBar, e.Which, true));
                
                // 关闭对话框
                ((AlertDialog)sender!).Dismiss();
            });
            
            builder.SetNegativeButton("取消", (sender, e) => { });
            builder.Show();
        }

        // 经络递增方法
        private bool IncrementMeridian()
        {
            if (_meridianSeekBar == null || _meridianNames.Count == 0) return false;
            
            var currentProgress = _meridianSeekBar.Progress;
            if (currentProgress < _meridianNames.Count - 1)
            {
                _meridianSeekBar.Progress = currentProgress + 1;
                OnMeridianSeekBarChanged(_meridianSeekBar, new SeekBar.ProgressChangedEventArgs(_meridianSeekBar, currentProgress + 1, true));
                return true;
            }
            return false;
        }

        // 经络递减方法
        private bool DecrementMeridian()
        {
            if (_meridianSeekBar == null || _meridianNames.Count == 0) return false;
            
            var currentProgress = _meridianSeekBar.Progress;
            if (currentProgress > 0)
            {
                _meridianSeekBar.Progress = currentProgress - 1;
                OnMeridianSeekBarChanged(_meridianSeekBar, new SeekBar.ProgressChangedEventArgs(_meridianSeekBar, currentProgress - 1, true));
                return true;
            }
            return false;
        }

        // 穴位递增方法
        private bool IncrementAcupoint()
        {
            if (_acupointSeekBar == null || _acupointNames.Count == 0) return false;
            
            var currentProgress = _acupointSeekBar.Progress;
            if (currentProgress < _acupointNames.Count - 1)
            {
                _acupointSeekBar.Progress = currentProgress + 1;
                OnAcupointSeekBarChanged(_acupointSeekBar, new SeekBar.ProgressChangedEventArgs(_acupointSeekBar, currentProgress + 1, true));
                return true;
            }
            return false;
        }

        // 穴位递减方法
        private bool DecrementAcupoint()
        {
            if (_acupointSeekBar == null || _acupointNames.Count == 0) return false;
            
            var currentProgress = _acupointSeekBar.Progress;
            if (currentProgress > 0)
            {
                _acupointSeekBar.Progress = currentProgress - 1;
                OnAcupointSeekBarChanged(_acupointSeekBar, new SeekBar.ProgressChangedEventArgs(_acupointSeekBar, currentProgress - 1, true));
                return true;
            }
            return false;
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

        public override void OnBackPressed()
        {
            Finish();
        }
    }
}