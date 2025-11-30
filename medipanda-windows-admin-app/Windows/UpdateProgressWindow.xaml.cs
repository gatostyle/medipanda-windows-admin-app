using System;
using System.ComponentModel;
using System.Windows;

namespace medipanda_windows_admin.Windows
{
    public partial class UpdateProgressWindow : Window
    {
        private DateTime _startTime;
        private bool _isCancelled = false;

        public bool IsCancelled => _isCancelled;

        public UpdateProgressWindow()
        {
            InitializeComponent();
            _startTime = DateTime.Now;
        }

        /// <summary>
        /// 진행률 업데이트
        /// </summary>
        public void UpdateProgress(int percent, long bytesReceived = 0, long totalBytes = 0)
        {
            Dispatcher.Invoke(() =>
            {
                DownloadProgress.Value = percent;
                PercentText.Text = $"{percent}%";

                // 크기 정보 표시
                if (totalBytes > 0)
                {
                    var received = FormatFileSize(bytesReceived);
                    var total = FormatFileSize(totalBytes);
                    SizeText.Text = $"{received} / {total}";
                }

                // 남은 시간 계산
                if (percent > 0 && percent < 100)
                {
                    var elapsed = DateTime.Now - _startTime;
                    var totalTime = TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds * 100 / percent);
                    var remaining = totalTime - elapsed;

                    if (remaining.TotalSeconds > 0)
                    {
                        if (remaining.TotalMinutes > 1)
                        {
                            StatusText.Text = $"남은 시간: 약 {(int)remaining.TotalMinutes}분 {remaining.Seconds}초";
                        }
                        else
                        {
                            StatusText.Text = $"남은 시간: 약 {(int)remaining.TotalSeconds}초";
                        }
                    }
                }

                // 완료
                if (percent >= 100)
                {
                    MainText.Text = "다운로드 완료!";
                    StatusText.Text = "설치 프로그램을 실행합니다...";
                    PercentText.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(34, 139, 34));
                    CancelButton.Visibility = Visibility.Collapsed;
                }
            });
        }

        /// <summary>
        /// 상태 메시지 업데이트
        /// </summary>
        public void UpdateStatus(string message)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = message;
            });
        }

        /// <summary>
        /// 메인 텍스트 업데이트
        /// </summary>
        public void UpdateMainText(string text)
        {
            Dispatcher.Invoke(() =>
            {
                MainText.Text = text;
            });
        }

        /// <summary>
        /// 파일 크기 포맷
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// 취소 버튼 클릭
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "업데이트를 취소하시겠습니까?",
                "확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _isCancelled = true;
                DialogResult = false;
                Close();
            }
        }

        /// <summary>
        /// 창 닫기 시
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_isCancelled && DownloadProgress.Value < 100)
            {
                var result = MessageBox.Show(
                    "업데이트가 진행 중입니다. 정말 취소하시겠습니까?",
                    "확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    _isCancelled = true;
                }
            }

            base.OnClosing(e);
        }
    }
}