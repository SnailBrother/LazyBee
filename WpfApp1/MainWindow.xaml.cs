using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Panuon.WPF.UI;
using Microsoft.Web.WebView2.Core;
using System.Windows.Media.Animation;
using Panuon.WPF;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : WindowX
    {
        private readonly HttpClient _httpClient = new();
        private List<WebsiteItem> _websiteItems = new(); // 存储从API获取的数据

        public class WebsiteItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Url { get; set; } = "";
            public string? Notes { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            Topmost = true; // 👈 关键代码：始终保持窗口置顶
            InitializeWebViewAsync();
            // 延迟启动滚动，确保布局已初始化
            Loaded += (s, e) => StartScrollingAd();

            // 加载搜索列表数据
            LoadSearchListAsync();
        }

        private async void LoadSearchListAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://121.4.22.55:5202/api/LazyBeewebsites");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    _websiteItems = JsonSerializer.Deserialize<List<WebsiteItem>>(json, options) ?? new List<WebsiteItem>();

                    // 可选：将数据转换为显示文本（URL或Name）
                    UpdateSearchList();
                }
                else
                {
                    // 如果API失败，使用默认列表
                    _websiteItems = GetDefaultWebsiteItems();
                    UpdateSearchList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载搜索列表失败: {ex.Message}");
                // 使用默认列表
                _websiteItems = GetDefaultWebsiteItems();
                UpdateSearchList();
            }
        }

        private List<WebsiteItem> GetDefaultWebsiteItems()
        {
            return new List<WebsiteItem>
            {
                new WebsiteItem { Id = 1, Name = "动漫视频", Url = "https://www.cnxgct.com/video/wanmeishijieguoyu-chendong/HeoYwS5JsH.html" },
                new WebsiteItem { Id = 2, Name = "音乐网站", Url = "http://121.4.22.55/app/music/home" },
                new WebsiteItem { Id = 3, Name = "Silence - Before You Exit", Url = "" },
                new WebsiteItem { Id = 4, Name = "Feels - WATTS/Khalid", Url = "" },
                new WebsiteItem { Id = 5, Name = "Shotgun - Us The Duo", Url = "" }
            };
        }

        private void UpdateSearchList()
        {
            // 这里可以根据需要选择显示URL还是Name
            // 方案1：显示URL（如果有的话）
            var searchList = _websiteItems
                .Select(item => !string.IsNullOrWhiteSpace(item.Url) ? item.Url : item.Name)
                .ToList();

            // 方案2：显示Name（URL）格式
            // var searchList = _websiteItems
            //     .Select(item => !string.IsNullOrWhiteSpace(item.Url) ? $"{item.Name} ({item.Url})" : item.Name)
            //     .ToList();

            // 方案3：优先显示URL，如果没有URL则显示Name
            // var searchList = _websiteItems
            //     .Select(item => string.IsNullOrWhiteSpace(item.Url) ? item.Name : item.Url)
            //     .ToList();

            // 将List<string>赋值给_searchList，这里你需要修改原来的_searchList变量
            // 由于原来的_searchList是readonly，我们需要创建一个新的属性或变量

            // 更新UI
            Dispatcher.Invoke(() =>
            {
                // 如果你想要立即刷新搜索框的内容
                SchBox.ItemsSource = searchList;
            });
        }

        #region 搜索功能
        private void SchBox_Opened(object sender, System.EventArgs e)
        {
            var searchBox = sender as SearchBox;
            // 使用从API获取的数据
            var searchList = _websiteItems
                .Select(item => !string.IsNullOrWhiteSpace(item.Url) ? item.Url : item.Name)
                .ToList();
            searchBox.ItemsSource = searchList;
        }

        private void SchBox_SearchTextChanged(object sender, SearchTextChangedRoutedEventArgs e)
        {
            var searchBox = sender as SearchBox;
            var searchText = e.Text?.Trim()?.ToLower();

            var allItems = _websiteItems
                .Select(item => new
                {
                    DisplayText = !string.IsNullOrWhiteSpace(item.Url) ? item.Url : item.Name,
                    OriginalItem = item
                })
                .ToList();

            if (string.IsNullOrEmpty(searchText))
            {
                searchBox.ItemsSource = allItems.Select(x => x.DisplayText).ToList();
            }
            else
            {
                var filtered = allItems
                    .Where(x => x.DisplayText.ToLower().Contains(searchText) ||
                               x.OriginalItem.Name.ToLower().Contains(searchText) ||
                               (!string.IsNullOrEmpty(x.OriginalItem.Url) &&
                                x.OriginalItem.Url.ToLower().Contains(searchText)))
                    .Select(x => x.DisplayText)
                    .ToList();
                searchBox.ItemsSource = filtered;
            }
        }

        private void SchBox_ItemClick(object sender, RoutedEventArgs e)
        {
            var searchBox = sender as Panuon.WPF.UI.SearchBox;
            if (searchBox == null) return;

            string selectedItem = searchBox.Text?.Trim();
            if (string.IsNullOrEmpty(selectedItem))
                return;

            // 查找对应的WebsiteItem
            var websiteItem = _websiteItems.FirstOrDefault(item =>
                (!string.IsNullOrWhiteSpace(item.Url) && item.Url.Equals(selectedItem)) ||
                item.Name.Equals(selectedItem) ||
                (!string.IsNullOrWhiteSpace(item.Url) && $"{item.Name} ({item.Url})".Equals(selectedItem)));

            string urlToNavigate = selectedItem;

            // 如果找到对应的WebsiteItem并且有URL，优先使用URL
            if (websiteItem != null && !string.IsNullOrWhiteSpace(websiteItem.Url))
            {
                urlToNavigate = websiteItem.Url;
            }

            // 自动补全协议
            if (!urlToNavigate.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !urlToNavigate.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                urlToNavigate = "https://" + urlToNavigate;
            }

            try
            {
                webView.Source = new Uri(urlToNavigate);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法加载网址：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavigateToUrl(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;

            // 自动补全协议
            if (!input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                input = "https://" + input;
            }

            try
            {
                webView.Source = new Uri(input);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法加载网址：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToUrl(SchBox.Text?.Trim());
        }
        #endregion

        // 以下是原有的方法保持不变
        private async void InitializeWebViewAsync()
        {
            try
            {
                await webView.EnsureCoreWebView2Async(null);
                webView.Source = new Uri("https://www.cnxgct.com/video/wanmeishijieguoyu-chendong/HeoYwS5JsH.html");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法初始化 WebView2: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TopAdText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dialog = new InputDialog(webView.Source?.ToString() ?? "");
            bool? result = dialog.ShowDialog();

            if (result == true && !string.IsNullOrWhiteSpace(dialog.InputUrl))
            {
                try
                {
                    webView.Source = new Uri(dialog.InputUrl);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法加载网址：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void StartScrollingAd()
        {
            var text = ScrollingAdText;
            var transform = AdTextTransform;

            // 获取文本实际宽度（近似）
            var formattedText = new FormattedText(
                text.Text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch),
                text.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                1.0
            );
            double textWidth = formattedText.Width;

            var duration = TimeSpan.FromSeconds((300 + textWidth) / 50.0); // 速度：50像素/秒

            var animation = new DoubleAnimation
            {
                From = 300,                     // 从右侧开始
                To = -textWidth,                // 滚动到完全离开左侧
                Duration = duration,
                RepeatBehavior = RepeatBehavior.Forever
            };

            transform.BeginAnimation(TranslateTransform.XProperty, animation);
        }
    }
}