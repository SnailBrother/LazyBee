using System;
using System.Windows;
using System.Windows.Controls;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace WpfApp1
{
    public partial class UrlInputDialog : UserControl
    {
        public event Action<string>? OnConfirmed; // 成功确认时触发
        public event Action? OnCanceled;         // 取消时触发

        private readonly HttpClient _httpClient = new();

        public string DefaultUrl
        {
            get => UrlInput.Text;
            set
            {
                UrlInput.Text = value;
                UrlInput.SelectAll();
                UrlInput.Focus();
            }
        }

        public UrlInputDialog()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadWebsitesAsync();
        }

        // ========== 原有逻辑迁移 ==========
        public class WebsiteItem
        {
            public int Id { get; set; }
            [JsonPropertyName("name")] public string Name { get; set; } = "";
            [JsonPropertyName("url")] public string Url { get; set; } = "";
            [JsonPropertyName("notes")] public string? Notes { get; set; }
        }

        private async Task LoadWebsitesAsync()
        {
            try
            {
                QuickButtonsPanel.Children.Clear();
                var response = await _httpClient.GetAsync("http://121.4.22.55:5202/api/LazyBeewebsites"); 
                if (!response.IsSuccessStatusCode)
                {
                    QuickButtonsPanel.Children.Add(new TextBlock { Text = "加载失败", Foreground = Brushes.Red });
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var websites = JsonSerializer.Deserialize<List<WebsiteItem>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (websites == null || websites.Count == 0)
                {
                    QuickButtonsPanel.Children.Add(new TextBlock { Text = "暂无快捷网站", Foreground = Brushes.Gray });
                    return;
                }

                foreach (var site in websites)
                {
                    var btn = new Button
                    {
                        Content = $"🌐 {site.Name}",
                        Width = 100,
                        Margin = new Thickness(0, 0, 10, 0),
                        Tag = site.Url,
                        ToolTip = site.Url
                    };
                    btn.Click += (s, e) =>
                    {
                        if (s is Button b && b.Tag is string url)
                        {
                            UrlInput.Text = url;
                            UrlInput.SelectAll();
                            UrlInput.Focus();
                        }
                    };
                    QuickButtonsPanel.Children.Add(btn);
                }
            }
            catch (Exception ex)
            {
                QuickButtonsPanel.Children.Add(new TextBlock { Text = $"错误: {ex.Message}", Foreground = Brushes.Red });
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var url = UrlInput.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("请输入网址", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                url = "https://" + url;

            try
            {
                _ = new Uri(url);
                OnConfirmed?.Invoke(url);
            }
            catch
            {
                MessageBox.Show("网址格式无效", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            OnCanceled?.Invoke();
        }
    }
}