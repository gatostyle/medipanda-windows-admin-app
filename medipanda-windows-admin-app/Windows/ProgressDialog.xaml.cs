using System.Windows;

namespace medipanda_windows_admin.Windows
{
    public partial class ProgressDialog : Window
    {
        public ProgressDialog(string message = "처리 중입니다...")
        {
            InitializeComponent();
            MessageText.Text = message;
        }

        public void UpdateMessage(string message)
        {
            Dispatcher.Invoke(() => MessageText.Text = message);
        }

        public void SetProgress(int percent)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = percent;
            });
        }
    }
}