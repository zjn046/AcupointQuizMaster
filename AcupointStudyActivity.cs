using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Views;
using Android.Views.Accessibility;
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
        private Spinner? _meridianSpinner;
        private Spinner? _acupointSpinner;
        private TextView? _acupointDetailText;
        private Button? _backButton;
        
        private BankInfo? _selectedBank;
        private readonly List<string> _meridianNames = new List<string>();
        private readonly List<string> _acupointNames = new List<string>();
        
        // 无障碍功能支持
        private bool _isAccessibilityEnabled = false;
        
        // 无障碍动作ID基础值
        private const int BaseActionIdMeridian = 0x10000;
        private const int BaseActionIdAcupoint = 0x20000;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            try
            {
                SetContentView(Resource.Layout.activity_acupoint_study);
                InitializeServices();
                InitializeUI();
                LoadAvailableBanks();
                SetupAccessibilityFeatures();
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

        private void SetupAccessibilityFeatures()
        {
            // 检测是否启用了无障碍服务
            var accessibilityManager = GetSystemService(AccessibilityService) as AccessibilityManager;
            _isAccessibilityEnabled = accessibilityManager?.IsEnabled ?? false;
            
            if (_isAccessibilityEnabled)
            {
                System.Diagnostics.Debug.WriteLine("TalkBack无障碍功能已启用，开始设置无障碍动作");
            }
        }

        private void InitializeUI()
        {
            _titleText = FindViewById<TextView>(Resource.Id.titleText);
            _meridianSpinner = FindViewById<Spinner>(Resource.Id.meridianSpinner);
            _acupointSpinner = FindViewById<Spinner>(Resource.Id.acupointSpinner);
            _acupointDetailText = FindViewById<TextView>(Resource.Id.acupointDetailText);
            _backButton = FindViewById<Button>(Resource.Id.backButton);

            // 设置基础无障碍支持
            if (_meridianSpinner != null) 
            {
                _meridianSpinner.ContentDescription = "选择要学习的经络，当前未选择";
                _meridianSpinner.ItemSelected += OnMeridianSelected;
            }
            
            if (_acupointSpinner != null) 
            {
                _acupointSpinner.ContentDescription = "选择要学习的穴位，请先选择经络";
                _acupointSpinner.ItemSelected += OnAcupointSelected;
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
                SetupMeridianSpinner();
                
                // 重新设置无障碍动作（因为数据已加载）
                if (_isAccessibilityEnabled)
                {
                    SetupMeridianAccessibilityActions();
                }
            }
            catch (SystemException ex)
            {
                ShowError("加载题库失败", ex.Message);
            }
        }

        private void SetupMeridianSpinner()
        {
            if (_meridianSpinner == null) return;

            var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, _meridianNames);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            _meridianSpinner.Adapter = adapter;
        }

        private void OnMeridianSelected(object? sender, AdapterView.ItemSelectedEventArgs e)
        {
            try
            {
                if (e.Position < 0 || e.Position >= _availableBanks.Count) return;

                _selectedBank = _availableBanks[e.Position];
                
                // 更新无障碍描述
                if (_meridianSpinner != null)
                {
                    _meridianSpinner.ContentDescription = $"选择要学习的经络，当前选择：{_selectedBank.Name}";
                }
                
                // 更新穴位选择器
                UpdateAcupointSpinner();
                
                // 清空详情显示
                _acupointDetailText?.SetText("请选择要学习的穴位", TextView.BufferType.Normal);
            }
            catch (SystemException ex)
            {
                ShowError("经络选择失败", ex.Message);
            }
        }

        private void UpdateAcupointSpinner()
        {
            if (_acupointSpinner == null || _selectedBank == null) return;

            _acupointNames.Clear();
            _acupointNames.AddRange(_selectedBank.AcupointNames);

            var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, _acupointNames);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            _acupointSpinner.Adapter = adapter;
            
            // 更新穴位选择器的无障碍描述
            _acupointSpinner.ContentDescription = $"选择要学习的穴位，当前经络：{_selectedBank.Name}，共{_acupointNames.Count}个穴位";
            
            // 重新设置穴位的无障碍动作
            if (_isAccessibilityEnabled)
            {
                SetupAcupointAccessibilityActions();
            }
        }

        private void OnAcupointSelected(object? sender, AdapterView.ItemSelectedEventArgs e)
        {
            try
            {
                if (e.Position < 0 || e.Position >= _acupointNames.Count || _selectedBank == null) return;

                var selectedAcupointName = _acupointNames[e.Position];
                
                // 更新穴位选择器的无障碍描述
                if (_acupointSpinner != null)
                {
                    _acupointSpinner.ContentDescription = $"选择要学习的穴位，当前选择：{selectedAcupointName}";
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

        // 设置经络选择器的无障碍支持
        private void SetupMeridianAccessibilityActions()
        {
            if (_meridianSpinner == null || !_isAccessibilityEnabled || _meridianNames.Count == 0) return;
            
            try
            {
                // 更新内容描述，包含所有可选经络的信息和选择指令  
                var availableMeridians = string.Join("，", _meridianNames);
                _meridianSpinner.ContentDescription = $"选择要学习的经络。可用经络有：{availableMeridians}。使用上下滑动切换选择，双击确认";
                
                // 设置为可聚焦，确保TalkBack可以找到
                _meridianSpinner.Focusable = true;
                _meridianSpinner.ImportantForAccessibility = ImportantForAccessibility.Yes;
                
                // 设置为可点击，启用交互功能
                _meridianSpinner.Clickable = true;
                
                System.Diagnostics.Debug.WriteLine($"已为经络选择器设置TalkBack支持，包含 {_meridianNames.Count} 个经络");
            }
            catch (SystemException ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置经络无障碍支持失败: {ex.Message}");
            }
        }

        // 设置穴位选择器的无障碍支持
        private void SetupAcupointAccessibilityActions()
        {
            if (_acupointSpinner == null || !_isAccessibilityEnabled || _acupointNames.Count == 0) return;
            
            try
            {
                // 更新内容描述，包含所有可选穴位的信息和选择指令
                var availableAcupoints = string.Join("，", _acupointNames);
                _acupointSpinner.ContentDescription = $"选择要学习的穴位。当前经络：{_selectedBank?.Name}。可用穴位有：{availableAcupoints}。使用上下滑动切换选择，双击确认";
                
                // 设置为可聚焦，确保TalkBack可以找到
                _acupointSpinner.Focusable = true;  
                _acupointSpinner.ImportantForAccessibility = ImportantForAccessibility.Yes;
                
                // 设置为可点击，启用交互功能
                _acupointSpinner.Clickable = true;
                
                System.Diagnostics.Debug.WriteLine($"已为穴位选择器设置TalkBack支持，包含 {_acupointNames.Count} 个穴位");
            }
            catch (SystemException ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置穴位无障碍支持失败: {ex.Message}");
            }
        }

        // 经络选择方法
        public void SelectMeridian(int index)
        {
            if (_meridianSpinner == null || index < 0 || index >= _availableBanks.Count) return;
            
            try
            {
                _meridianSpinner.SetSelection(index);
                
                // 发出无障碍提示
                var selectedName = _meridianNames[index];
                _meridianSpinner.AnnounceForAccessibility($"已选择经络：{selectedName}");
                
                System.Diagnostics.Debug.WriteLine($"通过TalkBack动作选择了经络：{selectedName}");
            }
            catch (SystemException ex)
            {
                System.Diagnostics.Debug.WriteLine($"选择经络失败: {ex.Message}");
            }
        }

        // 穴位选择方法
        public void SelectAcupoint(int index)
        {
            if (_acupointSpinner == null || index < 0 || index >= _acupointNames.Count) return;
            
            try
            {
                _acupointSpinner.SetSelection(index);
                
                // 发出无障碍提示
                var selectedName = _acupointNames[index];
                _acupointSpinner.AnnounceForAccessibility($"已选择穴位：{selectedName}");
                
                System.Diagnostics.Debug.WriteLine($"通过TalkBack动作选择了穴位：{selectedName}");
            }
            catch (SystemException ex)
            {
                System.Diagnostics.Debug.WriteLine($"选择穴位失败: {ex.Message}");
            }
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