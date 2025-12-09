using medipanda_windows_admin.Services;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace medipanda_windows_admin.Windows
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            SetVersionInfo();

            this.Loaded += (s, e) =>
            {
                AppConfig.IsDevMode = false;
                // 현재 모드에 맞게 ComboBox 선택
                EnvironmentComboBox.SelectedIndex = AppConfig.IsDevMode ? 1 : 0;
            };
        }

        private void EnvironmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EnvironmentComboBox.SelectedItem is ComboBoxItem selected)
            {
                bool isDevMode = selected.Tag?.ToString() == "Development";
                AppConfig.IsDevMode = isDevMode;

                if (isDevMode)
                {
                    UsernameTextBox.Text = "super";
                    PasswordBox.Password = "super";
                }
                else
                {
                    UsernameTextBox.Text = "";
                    PasswordBox.Password = "";
                }
            }
        }

        private void SetVersionInfo()
        {
            try
            {
                var version = UpdateService.Instance.GetCurrentVersion();
                VersionText.Text = $"v{version}";
            }
            catch
            {
                VersionText.Text = "v1.0.0";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DragMove();
            }
        }

        private void UsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UsernamePlaceholder.Visibility = string.IsNullOrEmpty(UsernameTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;

            CheckLoginButtonEnabled();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordPlaceholder.Visibility = string.IsNullOrEmpty(PasswordBox.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;

            CheckLoginButtonEnabled();
        }

        private void CheckLoginButtonEnabled()
        {
            LoginButton.IsEnabled = !string.IsNullOrWhiteSpace(UsernameTextBox.Text)
                                    && !string.IsNullOrWhiteSpace(PasswordBox.Password);
        }

        public void SetCredentials(string username, string password)
        {
            UsernameTextBox.Text = username;
            PasswordBox.Password = password;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var userId = UsernameTextBox.Text;
            var password = PasswordBox.Password;

            try
            {
                IsEnabled = false;
                Mouse.OverrideCursor = Cursors.Wait;

                var loginResponse = await AuthService.Instance.LoginAsync(userId, password);
                var userInfo = await AuthService.Instance.GetUserInfoAsync();

                if (userInfo.Role != "ADMIN" && userInfo.Role != "SUPER_ADMIN")
                {
                    MessageBox.Show("관리자만 이용이 가능합니다.", "접근 제한",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    TokenService.Instance.ClearTokens();
                    return;
                }

                UserSessionService.Instance.CurrentUser = userInfo;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message.Contains("401") || ex.Message.Contains("404")
                    ? "존재하지 않는 계정입니다."
                    : $"로그인 실패:\n{ex.Message}";
                MessageBox.Show(errorMessage, "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }
    }
}