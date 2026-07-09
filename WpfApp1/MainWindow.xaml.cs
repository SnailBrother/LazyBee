using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Panuon.WPF.UI;

namespace WpfApp1
{
    public partial class MainWindow : WindowX
    {
        private const string ApiBaseUrl = "https://www.cqzypg.online";

        // 配置文件路径
        private const string SettingsDir = @"C:\Program Files\Cqzypg\Settings";
        private const string SettingsFilePath = @"C:\Program Files\Cqzypg\Settings\appsettings.json";

        private readonly ImageSource _normalIcon =
            new BitmapImage(new Uri("pack://application:,,,/WpfApp1;component/Resources/Images/head.ico", UriKind.Absolute));

        private readonly ImageSource _highlightIcon =
            new BitmapImage(new Uri("pack://application:,,,/WpfApp1;component/Resources/Images/head_highlight.ico", UriKind.Absolute));

        private DispatcherTimer? _trayBlinkTimer;
        private bool _isHighlightIcon;

        private DispatcherTimer? _unreadPollTimer;
        private static readonly HttpClient _httpClient = new HttpClient();

        private bool _hasUnreadMessage = false;
        private bool _isPolling = false;

        // 当前设置
        private AppSettings _settings = new AppSettings();

        public MainWindow()
        {
            InitializeComponent();

            SetWindowIcon();

            // 读取/创建配置
            LoadOrCreateSettings();

            // 应用配置
            ApplySettingsToWindow();

            InitializeWebViewAsync();
            InitializeUnreadPolling();
        }

        private void SetWindowIcon()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/WpfApp1;component/Resources/Images/head.ico", UriKind.Absolute);
                this.Icon = BitmapFrame.Create(uri);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置任务栏图标失败: {ex.Message}");
            }
        }

        private async void InitializeWebViewAsync()
        {
            try
            {
                await webView.EnsureCoreWebView2Async(null);
                webView.Source = new Uri(ApiBaseUrl + "/");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法初始化 WebView2: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeUnreadPolling()
        {
            _unreadPollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _unreadPollTimer.Tick += async (_, __) => await PollUnreadStateAsync();
            _unreadPollTimer.Start();

            _ = PollUnreadStateAsync();
        }

        private async System.Threading.Tasks.Task PollUnreadStateAsync()
        {
            if (_isPolling) return;
            _isPolling = true;

            try
            {
                if (string.IsNullOrWhiteSpace(_settings.UserEmail))
                    return;

                string url = $"{ApiBaseUrl}/api/messages/unread-counts?userEmail={Uri.EscapeDataString(_settings.UserEmail)}";

                using var response = await _httpClient.GetAsync(url);
                string json = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"[Unread API] Status={(int)response.StatusCode}, Body={json}");

                if (!response.IsSuccessStatusCode) return;

                var dict = JsonSerializer.Deserialize<Dictionary<string, int>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                bool hasUnread = false;
                if (dict != null)
                {
                    foreach (var kv in dict)
                    {
                        if (kv.Value > 0)
                        {
                            hasUnread = true;
                            break;
                        }
                    }
                }

                SetUnreadState(hasUnread);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Unread API] Exception: {ex.Message}");
            }
            finally
            {
                _isPolling = false;
            }
        }

        private void SetUnreadState(bool hasUnread)
        {
            _hasUnreadMessage = hasUnread;

            bool isInTrayMode = this.Visibility != Visibility.Visible || MyTrayIcon.Visibility == Visibility.Visible;

            if (_hasUnreadMessage)
            {
                // 托盘模式下图标闪烁
                if (isInTrayMode) StartTrayBlink();
                else StopTrayBlink();

                // 无论是否托盘，只要有未读就闪任务栏按钮
                FlashTaskbar();
            }
            else
            {
                StopTrayBlink();
                StopFlashTaskbar();
            }
        }

        private void StartTrayBlink()
        {
            if (_trayBlinkTimer == null)
            {
                _trayBlinkTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                _trayBlinkTimer.Tick += (_, __) =>
                {
                    _isHighlightIcon = !_isHighlightIcon;
                    MyTrayIcon.IconSource = _isHighlightIcon ? _highlightIcon : _normalIcon;
                };
            }

            if (!_trayBlinkTimer.IsEnabled)
            {
                _isHighlightIcon = false;
                MyTrayIcon.IconSource = _normalIcon;
                _trayBlinkTimer.Start();
            }
        }

        private void StopTrayBlink()
        {
            if (_trayBlinkTimer != null)
                _trayBlinkTimer.Stop();

            _isHighlightIcon = false;
            MyTrayIcon.IconSource = _normalIcon;
        }

        private void HideButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            MyTrayIcon.Visibility = Visibility.Visible;

            if (_hasUnreadMessage) StartTrayBlink();
            else StopTrayBlink();
        }

        private void TrayIcon_OnLeftMouseDown(object sender, RoutedEventArgs e)
        {
            ShowMainWindowAndResetFlash();
        }

        private void ShowWindow_Click(object sender, RoutedEventArgs e)
        {
            ShowMainWindowAndResetFlash();
        }

        private void ShowMainWindowAndResetFlash()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();

            StopTrayBlink();
            StopFlashTaskbar();
            MyTrayIcon.Visibility = Visibility.Collapsed;
        }

        private void ExitApp_Click(object sender, RoutedEventArgs e)
        {
            CleanupAndExit();
            Application.Current.Shutdown();
        }

        // 设置按钮
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSettingsDialog();
        }

        protected override void OnClosed(EventArgs e)
        {
            CleanupAndExit();
            base.OnClosed(e);
        }

        private void CleanupAndExit()
        {
            try
            {
                _unreadPollTimer?.Stop();
                StopTrayBlink();
                StopFlashTaskbar();
                MyTrayIcon?.Dispose();
            }
            catch { }
        }

        // ===================== 设置文件读写 =====================
        private void LoadOrCreateSettings()
        {
            try
            {
                if (!Directory.Exists(SettingsDir))
                    Directory.CreateDirectory(SettingsDir);

                if (!File.Exists(SettingsFilePath))
                {
                    _settings = new AppSettings
                    {
                        UserEmail = "",
                        AlwaysOnTop = true
                    };
                    SaveSettings();
                    // 首次创建时弹设置
                    Dispatcher.BeginInvoke(new Action(ShowSettingsDialog), DispatcherPriority.Loaded);
                    return;
                }

                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _settings = settings ?? new AppSettings();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"读取设置失败: {ex.Message}");
                _settings = new AppSettings();
                Dispatcher.BeginInvoke(new Action(ShowSettingsDialog), DispatcherPriority.Loaded);
            }
        }

        private void SaveSettings()
        {
            try
            {
                if (!Directory.Exists(SettingsDir))
                    Directory.CreateDirectory(SettingsDir);

                string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    $"无法写入配置文件：{SettingsFilePath}\n请以管理员身份运行，或改到有写权限目录。",
                    "权限不足",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplySettingsToWindow()
        {
            this.Topmost = _settings.AlwaysOnTop;
        }

        // 简单设置弹窗（纯代码，无需新增 XAML 文件）
        private void ShowSettingsDialog()
        {
            var dialog = new Window
            {
                Title = "设置",
                Width = 420,
                Height = 220,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            var root = new System.Windows.Controls.Grid { Margin = new Thickness(16) };
            root.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });

            var emailLabel = new System.Windows.Controls.TextBlock
            {
                Text = "邮箱：",
                VerticalAlignment = VerticalAlignment.Center
            };
            System.Windows.Controls.Grid.SetRow(emailLabel, 0);
            root.Children.Add(emailLabel);

            var emailBox = new System.Windows.Controls.TextBox
            {
                Margin = new Thickness(0, 8, 0, 12),
                Text = _settings.UserEmail ?? "",
                Height = 28
            };
            System.Windows.Controls.Grid.SetRow(emailBox, 1);
            root.Children.Add(emailBox);

            var topmostCheck = new System.Windows.Controls.CheckBox
            {
                Content = "桌面始终显示在最前面（Topmost）",
                IsChecked = _settings.AlwaysOnTop,
                Margin = new Thickness(0, 0, 0, 12)
            };
            System.Windows.Controls.Grid.SetRow(topmostCheck, 2);
            root.Children.Add(topmostCheck);

            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var cancelBtn = new System.Windows.Controls.Button
            {
                Content = "取消",
                Width = 80,
                Height = 30,
                Margin = new Thickness(0, 0, 8, 0)
            };
            cancelBtn.Click += (_, __) => dialog.Close();

            var saveBtn = new System.Windows.Controls.Button
            {
                Content = "保存",
                Width = 80,
                Height = 30
            };
            saveBtn.Click += (_, __) =>
            {
                string email = (emailBox.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(email))
                {
                    MessageBox.Show("邮箱不能为空。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _settings.UserEmail = email;
                _settings.AlwaysOnTop = topmostCheck.IsChecked == true;

                SaveSettings();
                ApplySettingsToWindow();

                _ = PollUnreadStateAsync(); // 保存后立刻刷新一次未读
                dialog.Close();
            };

            buttonPanel.Children.Add(cancelBtn);
            buttonPanel.Children.Add(saveBtn);

            System.Windows.Controls.Grid.SetRow(buttonPanel, 3);
            root.Children.Add(buttonPanel);

            dialog.Content = root;
            dialog.ShowDialog();
        }

        // ===================== 任务栏闪烁 =====================
        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        [DllImport("user32.dll")]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        private const uint FLASHW_STOP = 0;
        private const uint FLASHW_CAPTION = 0x00000001;
        private const uint FLASHW_TRAY = 0x00000002;
        private const uint FLASHW_ALL = FLASHW_CAPTION | FLASHW_TRAY;
        private const uint FLASHW_TIMERNOFG = 0x0000000C;

        private void FlashTaskbar()
        {
            try
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                IntPtr hWnd = helper.Handle;
                if (hWnd == IntPtr.Zero) return;

                FLASHWINFO fw = new FLASHWINFO
                {
                    cbSize = Convert.ToUInt32(Marshal.SizeOf<FLASHWINFO>()),
                    hwnd = hWnd,
                    dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG, // 持续闪烁直到窗口前台
                    uCount = uint.MaxValue,
                    dwTimeout = 0
                };
                FlashWindowEx(ref fw);
            }
            catch { }
        }

        private void StopFlashTaskbar()
        {
            try
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                IntPtr hWnd = helper.Handle;
                if (hWnd == IntPtr.Zero) return;

                FLASHWINFO fw = new FLASHWINFO
                {
                    cbSize = Convert.ToUInt32(Marshal.SizeOf<FLASHWINFO>()),
                    hwnd = hWnd,
                    dwFlags = FLASHW_STOP,
                    uCount = 0,
                    dwTimeout = 0
                };
                FlashWindowEx(ref fw);
            }
            catch { }
        }
    }

    public class AppSettings
    {
        public string UserEmail { get; set; } = "";
        public bool AlwaysOnTop { get; set; } = true;
    }
}