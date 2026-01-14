using System;
using System.Windows;
using Panuon.WPF.UI;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Text.Json.Serialization;

namespace WpfApp1
{
    public partial class InputDialog : WindowX
    { 
        public string InputUrl { get; private set; }

        private readonly HttpClient _httpClient = new();

        public InputDialog(string defaultUrl = "")
        {
            InitializeComponent();
            UrlInput.Text = defaultUrl;
            UrlInput.SelectAll();
            UrlInput.Focus();
            Topmost = true;
            // 异步加载网站列表
            this.Loaded += async (s, e) => await LoadWebsitesAsync();
        }

        public class WebsiteItem
        {
            public int Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; } = "";

            [JsonPropertyName("url")]
            public string Url { get; set; } = "";

            [JsonPropertyName("notes")]
            public string? Notes { get; set; }
        }

        private async Task LoadWebsitesAsync()
        {
            // 注意：这里直接使用 QuickButtonsPanel 变量（因为 XAML 中有 x:Name）
            try
            {
                // 清空原有按钮
                QuickButtonsPanel.Children.Clear();

                var response = await _httpClient.GetAsync("http://121.4.22.55:5202/api/LazyBeewebsites");
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show("无法从服务器加载网站列表，请检查网络或服务状态。",
                        "加载失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var websites = JsonSerializer.Deserialize<List<WebsiteItem>>(json);

                if (websites == null || websites.Count == 0)
                {
                    QuickButtonsPanel.Children.Add(new TextBlock
                    {
                        Text = "暂无快捷网站",
                        Foreground = System.Windows.Media.Brushes.Gray
                    });
                    return;
                }

                // 添加按钮
                foreach (var site in websites)
                {
                    var btn = new Button
                    {
                        Content = $"🌐 {site.Name}",
                        Width = 100,
                        Margin = new Thickness(0, 0, 10, 0),
                        Tag = site.Url,
                        ToolTip = site.Url // 添加提示显示完整网址
                    };
                    btn.Click += QuickSelect_Click;
                    QuickButtonsPanel.Children.Add(btn);
                }
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"网络连接失败：{ex.Message}", "网络错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"数据格式错误：{ex.Message}", "数据错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载网站列表时出错：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ValidateAndConfirm(UrlInput.Text);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void QuickSelect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                string url = button.Tag.ToString();
                if (!string.IsNullOrEmpty(url))
                {
                    // 将网址填入输入框
                    UrlInput.Text = url;
                    UrlInput.SelectAll();
                    UrlInput.Focus();

                    // 或者直接确认：
                    // ValidateAndConfirm(url);
                }
            }
        }

        private void ValidateAndConfirm(string rawUrl)
        {
            var url = rawUrl.Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("请输入一个有效的网址。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 补全协议头
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https://" + url;
            }

            try
            {
                _ = new Uri(url); // 验证格式
                InputUrl = url;
                DialogResult = true;
                Close();
            }
            catch (UriFormatException)
            {
                MessageBox.Show("请输入有效的网址格式（例如：https://www.example.com）",
                    "格式错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}