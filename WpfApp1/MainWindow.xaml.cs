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
 

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : WindowX
    {
        public MainWindow()
        {
            InitializeComponent();
            Topmost = true; // 👈 关键代码：始终保持窗口置顶
            InitializeWebViewAsync();
            // 延迟启动滚动，确保布局已初始化
            Loaded += (s, e) => StartScrollingAd();
        }

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

        #region  搜索按钮
        //搜索按钮
        private readonly List<string> _searchList = new List<string>()
{
    "https://www.cnxgct.com/video/wanmeishijieguoyu-chendong/HeoYwS5JsH.html",
    "http://121.4.22.55/app/music/home",
    "Slience - Before You Exit",
    "Feels - WATTS/Khalid",
    "Shotgun - Us The Duo"
};
        private void SchBox_Opened(object sender, System.EventArgs e)
        {
            var searchBox = sender as SearchBox;
            searchBox.ItemsSource = _searchList.ToList();
        }
        private void SchBox_SearchTextChanged(object sender, SearchTextChangedRoutedEventArgs e)
        {
            var searchBox = sender as SearchBox;
            var searchText = e.Text?.Trim()?.ToLower();

            searchBox.ItemsSource = string.IsNullOrEmpty(searchText)
            ? _searchList
            : _searchList.Where(x => x.ToLower().Contains(searchText)).ToList();
        }
      
            private void SchBox_ItemClick(object sender, RoutedEventArgs e)
            {
                var searchBox = sender as Panuon.WPF.UI.SearchBox;
                if (searchBox == null) return;

                string input = searchBox.Text?.Trim(); // 👈 关键：用 Text 而不是 SelectedItem

                if (string.IsNullOrEmpty(input))
                    return;

                // 自动补全协议（如果需要）
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


    }
}