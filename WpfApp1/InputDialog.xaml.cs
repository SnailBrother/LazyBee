using System;
using System.Windows;
using Panuon.WPF.UI;

namespace WpfApp1
{
    public partial class InputDialog : WindowX
    {
        public string InputUrl { get; private set; }

        public InputDialog(string defaultUrl = "")
        {
            InitializeComponent();
            UrlInput.Text = defaultUrl;
            UrlInput.SelectAll();
            UrlInput.Focus();
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
            if (sender is System.Windows.Controls.Button button)
            {
                string url = button.Tag?.ToString();
                if (!string.IsNullOrEmpty(url))
                {
                    // 可选：直接确认，不经过输入框验证
                    ValidateAndConfirm(url);
                }
            }
        }

        private void ValidateAndConfirm(string rawUrl)
        {
            var url = rawUrl.Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("请输入一个有效的网址。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                MessageBox.Show("请输入有效的网址格式（例如：https://www.example.com）", "格式错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}