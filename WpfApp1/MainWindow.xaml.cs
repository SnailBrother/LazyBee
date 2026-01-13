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
    }
}