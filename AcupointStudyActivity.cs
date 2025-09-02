using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Views;
using Android.Views.Accessibility;
using AndroidX.Core.View;
using AndroidX.Core.View.Accessibility;
using AcupointQuizMaster.Models;
using AcupointQuizMaster.Services;

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
        
        // 无障碍动作ID
        public const int ActionSelectNextMeridian = 0x10000;
        public const int ActionSelectPreviousMeridian = 0x10001;
        public const int ActionSelectNextAcupoint = 0x10002;
        public const int ActionSelectPreviousAcupoint = 0x10003;

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
            catch (Exception ex)
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
                SetupAccessibilityActions();
            }
        }

        private void InitializeUI()
        {
            _titleText = FindViewById<TextView>(Resource.Id.titleText);
            _meridianSpinner = FindViewById<Spinner>(Resource.Id.meridianSpinner);
            _acupointSpinner = FindViewById<Spinner>(Resource.Id.acupointSpinner);
            _acupointDetailText = FindViewById<TextView>(Resource.Id.acupointDetailText);
            _backButton = FindViewById<Button>(Resource.Id.backButton);

            // 设置无障碍支持
            if (_meridianSpinner != null) 
            {
                _meridianSpinner.ContentDescription = "选择要学习的经络，当前未选择";
                _meridianSpinner.ItemSelected += OnMeridianSelected;
                
                // 设置无障碍动作
                SetupMeridianSpinnerAccessibility();
            }
            
            if (_acupointSpinner != null) 
            {
                _acupointSpinner.ContentDescription = "选择要学习的穴位，请先选择经络";
                _acupointSpinner.ItemSelected += OnAcupointSelected;
                
                // 设置无障碍动作
                SetupAcupointSpinnerAccessibility();
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
                    catch (Exception ex)
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
            }
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
        
        private void SetupAccessibilityActions()
        {
            // 为经络和穴位选择器设置无障碍动作
            SetupMeridianSpinnerAccessibility();
            SetupAcupointSpinnerAccessibility();
        }
        
        private void SetupMeridianSpinnerAccessibility()
        {
            if (_meridianSpinner == null) return;
            
            // 添加"下一个经络"无障碍动作
            var nextMeridianAction = new AccessibilityNodeInfoCompat.AccessibilityActionCompat(
                ActionSelectNextMeridian, "下一个经络");
            ViewCompat.SetAccessibilityDelegate(_meridianSpinner, new NextMeridianDelegate(this));
            
            // 添加"上一个经络"无障碍动作
            var previousMeridianAction = new AccessibilityNodeInfoCompat.AccessibilityActionCompat(
                ActionSelectPreviousMeridian, "上一个经络");
        }
        
        private void SetupAcupointSpinnerAccessibility()
        {
            if (_acupointSpinner == null) return;
            
            ViewCompat.SetAccessibilityDelegate(_acupointSpinner, new NextAcupointDelegate(this));
        }
        
        public void SelectNextMeridian()
        {
            if (_meridianSpinner == null || _availableBanks.Count == 0) return;
            
            int currentPosition = _meridianSpinner.SelectedItemPosition;
            int nextPosition = (currentPosition + 1) % _availableBanks.Count;
            _meridianSpinner.SetSelection(nextPosition);
            
        }
        
        public void SelectPreviousMeridian()
        {
            if (_meridianSpinner == null || _availableBanks.Count == 0) return;
            
            int currentPosition = _meridianSpinner.SelectedItemPosition;
            int previousPosition = currentPosition - 1;
            if (previousPosition < 0)
                previousPosition = _availableBanks.Count - 1;
            
            _meridianSpinner.SetSelection(previousPosition);
            
        }
        
        public void SelectNextAcupoint()
        {
            if (_acupointSpinner == null || _acupointNames.Count == 0) return;
            
            int currentPosition = _acupointSpinner.SelectedItemPosition;
            int nextPosition = (currentPosition + 1) % _acupointNames.Count;
            _acupointSpinner.SetSelection(nextPosition);
            
        }
        
        public void SelectPreviousAcupoint()
        {
            if (_acupointSpinner == null || _acupointNames.Count == 0) return;
            
            int currentPosition = _acupointSpinner.SelectedItemPosition;
            int previousPosition = currentPosition - 1;
            if (previousPosition < 0)
                previousPosition = _acupointNames.Count - 1;
            
            _acupointSpinner.SetSelection(previousPosition);
            
        }
    }
    
    // 经络选择器无障碍委托
    public class NextMeridianDelegate : AccessibilityDelegateCompat
    {
        private readonly AcupointStudyActivity _activity;
        
        public NextMeridianDelegate(AcupointStudyActivity activity)
        {
            _activity = activity;
        }
        
        public override void OnInitializeAccessibilityNodeInfo(View? host, AccessibilityNodeInfoCompat? info)
        {
            base.OnInitializeAccessibilityNodeInfo(host, info);
            
            if (info != null)
            {
                info.AddAction(new AccessibilityNodeInfoCompat.AccessibilityActionCompat(
                    AcupointStudyActivity.ActionSelectNextMeridian, "下一个经络"));
                info.AddAction(new AccessibilityNodeInfoCompat.AccessibilityActionCompat(
                    AcupointStudyActivity.ActionSelectPreviousMeridian, "上一个经络"));
            }
        }
        
        public override bool PerformAccessibilityAction(View? host, int action, Bundle? args)
        {
            if (action == AcupointStudyActivity.ActionSelectNextMeridian)
            {
                _activity.SelectNextMeridian();
                return true;
            }
            else if (action == AcupointStudyActivity.ActionSelectPreviousMeridian)
            {
                _activity.SelectPreviousMeridian();
                return true;
            }
            return base.PerformAccessibilityAction(host, action, args);
        }
    }
    
    // 穴位选择器无障碍委托
    public class NextAcupointDelegate : AccessibilityDelegateCompat
    {
        private readonly AcupointStudyActivity _activity;
        
        public NextAcupointDelegate(AcupointStudyActivity activity)
        {
            _activity = activity;
        }
        
        public override void OnInitializeAccessibilityNodeInfo(View? host, AccessibilityNodeInfoCompat? info)
        {
            base.OnInitializeAccessibilityNodeInfo(host, info);
            
            if (info != null)
            {
                info.AddAction(new AccessibilityNodeInfoCompat.AccessibilityActionCompat(
                    AcupointStudyActivity.ActionSelectNextAcupoint, "下一个穴位"));
                info.AddAction(new AccessibilityNodeInfoCompat.AccessibilityActionCompat(
                    AcupointStudyActivity.ActionSelectPreviousAcupoint, "上一个穴位"));
            }
        }
        
        public override bool PerformAccessibilityAction(View? host, int action, Bundle? args)
        {
            if (action == AcupointStudyActivity.ActionSelectNextAcupoint)
            {
                _activity.SelectNextAcupoint();
                return true;
            }
            else if (action == AcupointStudyActivity.ActionSelectPreviousAcupoint)
            {
                _activity.SelectPreviousAcupoint();
                return true;
            }
            return base.PerformAccessibilityAction(host, action, args);
        }
    }
}