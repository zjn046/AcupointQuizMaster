using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
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
    }
}