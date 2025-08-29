using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using AcupointQuizMaster.Models;
using AcupointQuizMaster.Services;

namespace AcupointQuizMaster
{
    [Activity(Label = "设置")]
    public class SettingsActivity : Activity
    {
        private PersistenceService? _persistenceService;
        private AppSettings _currentSettings = new AppSettings();
        
        // UI控件
        private Spinner? _platformSpinner;
        private EditText? _apiUrlEditText;
        private EditText? _apiKeyEditText;
        private EditText? _modelNameEditText;
        private Button? _saveButton;
        private Button? _testButton;
        private Button? _resetButton;
        private Button? _backButton;
        private TextView? _statusText;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            try
            {
                SetContentView(Resource.Layout.activity_settings);
                InitializeServices();
                InitializeUI();
                LoadSettings();
            }
            catch (Exception ex)
            {
                ShowError("初始化失败", ex.Message);
                Finish();
            }
        }

        private void InitializeServices()
        {
            _persistenceService = new PersistenceService(this);
        }

        private void InitializeUI()
        {
            _platformSpinner = FindViewById<Spinner>(Resource.Id.platformSpinner);
            _apiUrlEditText = FindViewById<EditText>(Resource.Id.apiUrlEditText);
            _apiKeyEditText = FindViewById<EditText>(Resource.Id.apiKeyEditText);
            _modelNameEditText = FindViewById<EditText>(Resource.Id.modelNameEditText);
            _saveButton = FindViewById<Button>(Resource.Id.saveButton);
            _testButton = FindViewById<Button>(Resource.Id.testButton);
            _resetButton = FindViewById<Button>(Resource.Id.resetButton);
            _backButton = FindViewById<Button>(Resource.Id.backButton);
            _statusText = FindViewById<TextView>(Resource.Id.statusText);

            // 设置平台选择器
            SetupPlatformSpinner();

            // 绑定事件
            if (_saveButton != null) _saveButton.Click += OnSaveClick;
            if (_testButton != null) _testButton.Click += OnTestClick;
            if (_resetButton != null) _resetButton.Click += OnResetClick;
            if (_backButton != null) _backButton.Click += OnBackClick;
        }

        private void SetupPlatformSpinner()
        {
            if (_platformSpinner == null) return;

            var platforms = Enum.GetValues(typeof(AIPlatform)).Cast<AIPlatform>().ToArray();
            var configs = PlatformConfig.GetConfigs();
            var platformNames = platforms.Select(p => configs[p].Name).ToArray();

            var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, platformNames);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            _platformSpinner.Adapter = adapter;

            _platformSpinner.ItemSelected += OnPlatformSelected;
        }

        private void OnPlatformSelected(object? sender, AdapterView.ItemSelectedEventArgs e)
        {
            var platforms = Enum.GetValues(typeof(AIPlatform)).Cast<AIPlatform>().ToArray();
            var selectedPlatform = platforms[e.Position];
            
            var configs = PlatformConfig.GetConfigs();
            var config = configs[selectedPlatform];

            // 自动填充API URL和默认模型（除非是自定义平台）
            if (!config.RequiresCustomUrl)
            {
                _apiUrlEditText?.SetText(config.ApiUrl, TextView.BufferType.Normal);
                _apiUrlEditText!.Enabled = false;
            }
            else
            {
                _apiUrlEditText!.Enabled = true;
            }

            // 设置默认模型
            _modelNameEditText?.SetText(config.DefaultModel, TextView.BufferType.Normal);
        }

        private void LoadSettings()
        {
            try
            {
                if (_persistenceService == null) return;

                _currentSettings = _persistenceService.LoadSettings();
                
                // 设置平台选择器
                var platforms = Enum.GetValues(typeof(AIPlatform)).Cast<AIPlatform>().ToArray();
                var platformIndex = Array.IndexOf(platforms, _currentSettings.AiPlatform);
                if (platformIndex >= 0 && _platformSpinner != null)
                {
                    _platformSpinner.SetSelection(platformIndex);
                }
                
                // 填充UI控件
                _apiUrlEditText?.SetText(_currentSettings.ApiUrl, TextView.BufferType.Normal);
                _apiKeyEditText?.SetText(_currentSettings.ApiKey, TextView.BufferType.Normal);
                _modelNameEditText?.SetText(_currentSettings.ModelName, TextView.BufferType.Normal);

                UpdateStatus();
            }
            catch (Exception ex)
            {
                ShowError("加载设置失败", ex.Message);
            }
        }

        private void OnSaveClick(object? sender, EventArgs e)
        {
            try
            {
                if (_persistenceService == null) return;

                // 获取选择的平台
                var platforms = Enum.GetValues(typeof(AIPlatform)).Cast<AIPlatform>().ToArray();
                var selectedPlatform = platforms[_platformSpinner?.SelectedItemPosition ?? 0];

                // 从UI控件获取数据
                var newSettings = new AppSettings
                {
                    AiPlatform = selectedPlatform,
                    ApiUrl = _apiUrlEditText?.Text?.Trim() ?? string.Empty,
                    ApiKey = _apiKeyEditText?.Text?.Trim() ?? string.Empty,
                    ModelName = _modelNameEditText?.Text?.Trim() ?? string.Empty
                };

                // 验证设置
                if (!ValidateSettings(newSettings))
                {
                    return;
                }

                // 保存设置
                if (_persistenceService.SaveSettings(newSettings))
                {
                    _currentSettings = newSettings;
                    UpdateStatus("设置已保存");
                    ShowMessage("成功", "设置已保存成功！");
                }
                else
                {
                    ShowError("保存失败", "无法保存设置，请重试。");
                }
            }
            catch (Exception ex)
            {
                ShowError("保存失败", ex.Message);
            }
        }

        private async void OnTestClick(object? sender, EventArgs e)
        {
            try
            {
                // 获取当前界面的设置
                var platforms = Enum.GetValues(typeof(AIPlatform)).Cast<AIPlatform>().ToArray();
                var selectedPlatform = platforms[_platformSpinner?.SelectedItemPosition ?? 0];

                var testSettings = new AppSettings
                {
                    AiPlatform = selectedPlatform,
                    ApiUrl = _apiUrlEditText?.Text?.Trim() ?? string.Empty,
                    ApiKey = _apiKeyEditText?.Text?.Trim() ?? string.Empty,
                    ModelName = _modelNameEditText?.Text?.Trim() ?? string.Empty
                };

                if (!ValidateSettings(testSettings))
                {
                    return;
                }

                UpdateStatus("正在测试连接...");
                _testButton!.Enabled = false;

                // 创建AIService并测试连接
                using (var aiService = new AIService())
                {
                    // 临时设置API配置
                    aiService.UpdateSettings(testSettings);
                    
                    // 执行简单的测试请求
                    var testResult = await TestApiConnection(aiService);
                    
                    if (testResult)
                    {
                        UpdateStatus("连接测试成功！");
                        ShowMessage("测试成功", "API连接正常，可以正常使用AI功能。");
                    }
                    else
                    {
                        UpdateStatus("连接测试失败");
                        ShowError("测试失败", "API连接失败，请检查设置是否正确。");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("测试出错");
                ShowError("测试失败", $"测试连接时发生错误：{ex.Message}");
            }
            finally
            {
                _testButton!.Enabled = true;
            }
        }

        private void OnResetClick(object? sender, EventArgs e)
        {
            try
            {
                var defaultSettings = AppSettings.Default();
                
                _apiUrlEditText?.SetText(defaultSettings.ApiUrl, TextView.BufferType.Normal);
                _apiKeyEditText?.SetText(defaultSettings.ApiKey, TextView.BufferType.Normal);
                _modelNameEditText?.SetText(defaultSettings.ModelName, TextView.BufferType.Normal);

                UpdateStatus("已重置为默认设置");
                ShowMessage("重置成功", "设置已重置为默认值，请记得保存。");
            }
            catch (Exception ex)
            {
                ShowError("重置失败", ex.Message);
            }
        }

        private void OnBackClick(object? sender, EventArgs e)
        {
            Finish();
        }

        private bool ValidateSettings(AppSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.ApiUrl))
            {
                ShowError("验证失败", "API地址不能为空");
                return false;
            }

            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                ShowError("验证失败", "API密钥不能为空");
                return false;
            }

            if (string.IsNullOrWhiteSpace(settings.ModelName))
            {
                ShowError("验证失败", "模型名称不能为空");
                return false;
            }

            if (!Uri.TryCreate(settings.ApiUrl, UriKind.Absolute, out _))
            {
                ShowError("验证失败", "API地址格式不正确");
                return false;
            }

            return true;
        }

        private async Task<bool> TestApiConnection(AIService aiService)
        {
            try
            {
                // 创建一个简单的测试请求
                var testAcupoint = new AcupointInfo
                {
                    Location = "测试位置",
                    Treatment = "测试主治",
                    Meridian = "测试经络"
                };

                var (question, canonicalAnswer, questionType) = await aiService.BuildQuestionForcedAsync(
                    "测试穴位", testAcupoint, "定位");

                return !string.IsNullOrEmpty(question);
            }
            catch
            {
                return false;
            }
        }

        private void UpdateStatus(string message = "")
        {
            if (_statusText == null) return;

            if (string.IsNullOrEmpty(message))
            {
                if (_currentSettings.IsValid())
                {
                    _statusText.SetText("设置已配置，可以使用AI功能", TextView.BufferType.Normal);
                }
                else
                {
                    _statusText.SetText("请配置完整的API设置以启用AI测评功能", TextView.BufferType.Normal);
                }
            }
            else
            {
                _statusText.SetText(message, TextView.BufferType.Normal);
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

        public override void OnBackPressed()
        {
            Finish();
        }
    }
}