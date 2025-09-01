using System;
using System.Collections.Generic;
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
        private ConfigService? _configService;
        private AppSettings _currentSettings = new AppSettings();
        
        // UI控件
        private Spinner? _apiPlatformSpinner;
        private TextView? _apiUrlDisplay;
        private TextView? _apiKeyDisplay;
        private Spinner? _modelSpinner;
        private Button? _saveButton;
        private Button? _testButton;
        private Button? _resetButton;
        private Button? _backButton;
        private TextView? _statusText;

        private readonly List<ApiPlatform> _availablePlatforms = new List<ApiPlatform>();
        private readonly List<string> _availableModels = new List<string>();

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
            _configService = new ConfigService();
            
            // 加载可用平台
            _availablePlatforms.AddRange(ConfigService.AvailablePlatforms);
        }

        private void InitializeUI()
        {
            _apiPlatformSpinner = FindViewById<Spinner>(Resource.Id.apiPlatformSpinner);
            _apiUrlDisplay = FindViewById<TextView>(Resource.Id.apiUrlDisplay);
            _apiKeyDisplay = FindViewById<TextView>(Resource.Id.apiKeyDisplay);
            _modelSpinner = FindViewById<Spinner>(Resource.Id.modelSpinner);
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
            if (_apiPlatformSpinner != null) _apiPlatformSpinner.ItemSelected += OnPlatformSelected;
        }

        private void SetupPlatformSpinner()
        {
            if (_apiPlatformSpinner == null) return;

            var platformNames = _availablePlatforms.Select(p => p.Name).ToList();
            var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, platformNames);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            _apiPlatformSpinner.Adapter = adapter;
        }

        private void LoadSettings()
        {
            try
            {
                if (_persistenceService == null) return;

                _currentSettings = _persistenceService.LoadSettings();
                
                // 设置平台选择器的当前值
                SetSelectedPlatform(_currentSettings.ApiPlatform);
                
                // 加载并显示当前平台的配置信息
                LoadPlatformConfigAsync();

                UpdateStatus();
            }
            catch (Exception ex)
            {
                ShowError("加载设置失败", ex.Message);
            }
        }

        private void SetSelectedPlatform(string platformId)
        {
            if (_apiPlatformSpinner == null) return;
            
            var platformIndex = _availablePlatforms.FindIndex(p => p.Id == platformId);
            if (platformIndex >= 0)
            {
                _apiPlatformSpinner.SetSelection(platformIndex);
            }
        }

        private async void OnPlatformSelected(object? sender, AdapterView.ItemSelectedEventArgs e)
        {
            try
            {
                if (e.Position < 0 || e.Position >= _availablePlatforms.Count) return;

                var selectedPlatform = _availablePlatforms[e.Position];
                _currentSettings.ApiPlatform = selectedPlatform.Id;

                // 更新模型选择器
                UpdateModelSpinner(selectedPlatform);
                
                // 加载平台配置
                await LoadPlatformConfigAsync();
            }
            catch (Exception ex)
            {
                ShowError("平台选择失败", ex.Message);
            }
        }

        private void UpdateModelSpinner(ApiPlatform platform)
        {
            if (_modelSpinner == null) return;

            _availableModels.Clear();
            _availableModels.AddRange(platform.AvailableModels);

            var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, _availableModels);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            _modelSpinner.Adapter = adapter;

            // 选择当前模型，如果存在的话
            var currentModelIndex = _availableModels.IndexOf(_currentSettings.ModelName);
            if (currentModelIndex >= 0)
            {
                _modelSpinner.SetSelection(currentModelIndex);
            }
            else if (_availableModels.Count > 0)
            {
                _modelSpinner.SetSelection(0);
                _currentSettings.ModelName = _availableModels[0];
            }
        }

        private async Task LoadPlatformConfigAsync()
        {
            try
            {
                var selectedPlatform = _configService?.GetPlatformById(_currentSettings.ApiPlatform);
                if (selectedPlatform == null) return;

                if (string.IsNullOrEmpty(selectedPlatform.ConfigUrl))
                {
                    // 没有远程配置URL的平台，显示需要手动配置
                    _apiUrlDisplay?.SetText("需要手动配置", TextView.BufferType.Normal);
                    _apiKeyDisplay?.SetText("需要手动配置", TextView.BufferType.Normal);
                    UpdateStatus("此平台需要手动配置API信息");
                    return;
                }

                UpdateStatus("正在获取平台配置...");
                
                var remoteConfig = await _configService.GetRemoteConfigAsync(selectedPlatform.ConfigUrl);
                if (remoteConfig != null)
                {
                    _currentSettings.ApiUrl = remoteConfig.ApiUrl;
                    _currentSettings.ApiKey = remoteConfig.ApiKey;
                    
                    // 显示配置信息（隐藏部分API密钥）
                    _apiUrlDisplay?.SetText(remoteConfig.ApiUrl, TextView.BufferType.Normal);
                    var maskedApiKey = MaskApiKey(remoteConfig.ApiKey);
                    _apiKeyDisplay?.SetText(maskedApiKey, TextView.BufferType.Normal);
                    
                    UpdateStatus("已获取平台配置");
                }
                else
                {
                    _apiUrlDisplay?.SetText("获取配置失败", TextView.BufferType.Normal);
                    _apiKeyDisplay?.SetText("获取配置失败", TextView.BufferType.Normal);
                    UpdateStatus("获取平台配置失败，请检查网络连接");
                }
            }
            catch (Exception ex)
            {
                ShowError("获取配置失败", ex.Message);
            }
        }

        private string MaskApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey)) return "未设置";
            if (apiKey.Length <= 8) return "****";
            
            return apiKey.Substring(0, 4) + "****" + apiKey.Substring(apiKey.Length - 4);
        }

        private void OnSaveClick(object? sender, EventArgs e)
        {
            try
            {
                if (_persistenceService == null) return;

                // 获取选中的模型
                if (_modelSpinner != null && _modelSpinner.SelectedItemPosition >= 0 && 
                    _modelSpinner.SelectedItemPosition < _availableModels.Count)
                {
                    _currentSettings.ModelName = _availableModels[_modelSpinner.SelectedItemPosition];
                }

                // 验证设置
                if (!ValidateSettings(_currentSettings))
                {
                    return;
                }

                // 保存设置
                if (_persistenceService.SaveSettings(_currentSettings))
                {
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
                // 获取当前设置用于测试
                var testSettings = _currentSettings.Clone();
                
                // 获取选中的模型
                if (_modelSpinner != null && _modelSpinner.SelectedItemPosition >= 0 && 
                    _modelSpinner.SelectedItemPosition < _availableModels.Count)
                {
                    testSettings.ModelName = _availableModels[_modelSpinner.SelectedItemPosition];
                }

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
                _currentSettings = defaultSettings;
                
                // 重新设置UI
                SetSelectedPlatform(defaultSettings.ApiPlatform);
                LoadPlatformConfigAsync();

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
                ShowError("验证失败", "API地址不能为空，请选择一个有效的平台");
                return false;
            }

            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                ShowError("验证失败", "API密钥不能为空，请选择一个有效的平台");
                return false;
            }

            if (string.IsNullOrWhiteSpace(settings.ModelName))
            {
                ShowError("验证失败", "模型名称不能为空，请选择一个模型");
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