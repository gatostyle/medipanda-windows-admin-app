using medipanda_windows_admin;
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
        private string _easterEggInput = "";
        private const string EasterEggCode = "medipanda##";

        public LoginWindow()
        {
            InitializeComponent();

            // 버전 정보 설정
            SetVersionInfo();

            // 이스터에그를 위한 키 이벤트 등록
            this.KeyDown += LoginWindow_KeyDown;
            this.PreviewKeyDown += LoginWindow_PreviewKeyDown;

            // 포커스를 받을 수 있도록 설정
            this.Focusable = true;
            this.Focus();
        }

        private void LoginWindow_KeyDown(object sender, KeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"KeyDown 이벤트 발생: {e.Key}");
        }

        private void LoginWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 텍스트박스나 패스워드박스에 포커스가 있으면 무시
            if (UsernameTextBox.IsFocused || PasswordBox.IsFocused)
            {
                _easterEggInput = "";
                return;
            }

            // 입력된 키를 문자로 변환
            char inputChar = GetCharFromKey(e.Key, e);

            if (inputChar != '\0')
            {
                _easterEggInput += inputChar;

                // 디버깅용 (배포시 제거 가능)
                System.Diagnostics.Debug.WriteLine($"Easter Egg Input: {_easterEggInput}");

                // 입력 문자열이 너무 길어지면 앞부분 제거
                if (_easterEggInput.Length > EasterEggCode.Length)
                {
                    _easterEggInput = _easterEggInput.Substring(_easterEggInput.Length - EasterEggCode.Length);
                }

                // 이스터에그 코드 확인
                if (_easterEggInput == EasterEggCode)
                {
                    ToggleDevMode();
                    _easterEggInput = "";
                    e.Handled = true;
                }
            }
        }

        private char GetCharFromKey(Key key, KeyEventArgs e)
        {
            bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            // 문자 키 변환
            if (key >= Key.A && key <= Key.Z)
            {
                return (char)('a' + (key - Key.A));
            }

            // 숫자 키 변환
            if (key >= Key.D0 && key <= Key.D9)
            {
                int num = key - Key.D0;

                // Shift + 3 = #
                if (isShiftPressed && num == 3)
                {
                    return '#';
                }

                return (char)('0' + num);
            }

            // NumPad 숫자 키
            if (key >= Key.NumPad0 && key <= Key.NumPad9)
            {
                return (char)('0' + (key - Key.NumPad0));
            }

            return '\0'; // 처리할 수 없는 키
        }

        private void ToggleDevMode()
        {
            // Dev Mode 토글
            AppConfig.IsDevMode = !AppConfig.IsDevMode;

            // Dev Mode 텍스트 표시/숨김
            if (DevModeText != null)
            {
                DevModeText.Visibility = AppConfig.IsDevMode ? Visibility.Visible : Visibility.Collapsed;
            }

            // BaseApiService의 URL 업데이트를 위해 알림
            MessageBox.Show(
                AppConfig.IsDevMode ? "Dev Mode가 활성화되었습니다." : "Dev Mode가 비활성화되었습니다.",
                "모드 변경",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void SetVersionInfo()
        {
            try
            {
                // 방법 1: UpdateService 사용
                var version = UpdateService.Instance.GetCurrentVersion();
                VersionText.Text = $"v{version}";

                // 방법 2: Assembly 정보 직접 사용 (UpdateService 없이)
                // var assembly = Assembly.GetExecutingAssembly();
                // var version = assembly.GetName().Version;
                // VersionText.Text = $"v{version.Major}.{version.Minor}.{version.Build}";

                // 방법 3: 파일 버전 사용
                // var version = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;
                // VersionText.Text = $"v{version}";
            }
            catch
            {
                // 버전 정보를 가져올 수 없는 경우 기본값 표시
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

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Border 클릭 시 Window에 포커스 설정 (이스터에그 입력을 위해)
            this.Focus();
        }

        // Username Placeholder 처리 및 버튼 활성화 체크
        private void UsernameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UsernamePlaceholder.Visibility = string.IsNullOrEmpty(UsernameTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;

            CheckLoginButtonEnabled();
        }

        // Password Placeholder 처리 및 버튼 활성화 체크
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordPlaceholder.Visibility = string.IsNullOrEmpty(PasswordBox.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;

            CheckLoginButtonEnabled();
        }

        // 로그인 버튼 활성화 체크
        private void CheckLoginButtonEnabled()
        {
            LoginButton.IsEnabled = !string.IsNullOrWhiteSpace(UsernameTextBox.Text)
                                    && !string.IsNullOrWhiteSpace(PasswordBox.Password);
        }

        // 테스트용 자동 입력 메서드
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
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                // 1. 로그인 API 호출
                var loginResponse = await AuthService.Instance.LoginAsync(userId, password);

                // 2. 사용자 정보 조회
                var userInfo = await AuthService.Instance.GetUserInfoAsync();

                // 3. 파트너 상태 체크
                if (userInfo.Role != "ADMIN" && userInfo.Role != "SUPER_ADMIN")
                {
                    System.Windows.MessageBox.Show("관리자만 이용이 가능합니다.", "접근 제한",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    // 토큰 삭제
                    TokenService.Instance.ClearTokens();
                    return;
                }

                // 4. 사용자 정보를 글로벌 세션에 저장
                UserSessionService.Instance.CurrentUser = userInfo;
                /*
                // 5. 딜러 및 거래처 데이터 로드
                try
                {
                    await DealerService.Instance.LoadAllDataAsync();
                }
                catch (Exception dataEx)
                {
                    System.Windows.MessageBox.Show($"데이터 로드 중 오류가 발생했습니다:\n{dataEx.Message}", "경고",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    // 데이터 로드 실패해도 로그인은 진행
                }
                */
                // 6. 로그인 성공
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                // 에러 메시지에 따라 다른 메시지 표시
                string errorMessage = ex.Message.Contains("401") || ex.Message.Contains("404")
                    ? "존재하지 않는 계정입니다."
                    : $"로그인 실패:\n{ex.Message}";
                System.Windows.MessageBox.Show(errorMessage, "오류",
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