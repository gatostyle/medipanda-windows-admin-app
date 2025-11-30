using medipanda_windows_admin.Services;
using medipanda_windows_admin.Windows;
using System.Windows;

namespace medipanda_windows_admin
{
    public partial class App : System.Windows.Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 명시적으로 Shutdown을 호출할 때까지 앱이 종료되지 않도록 설정
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 1. 업데이트 체크
            if (!await CheckForUpdatesAsync())
            {
                // 업데이트 취소 또는 실패 시 앱 종료
                Shutdown();
                return;
            }

            // 2. 로그인 창 표시
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // 테스트용 자동 로그인 (프로덕션에서는 제거)
            loginWindow.SetCredentials("super", "super");
            AppConfig.IsDevMode = true;

            if (loginWindow.ShowDialog() == true)
            {
                // 로그인 성공 시 MainWindow가 닫힐 때 앱 종료되도록 변경
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;

                MainWindow mainWindow = new MainWindow();
                mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                this.MainWindow = mainWindow;
                mainWindow.Show();
            }
            else
            {
                Shutdown();
            }
        }

        private async Task<bool> CheckForUpdatesAsync()
        {
            try
            {
                var updateService = UpdateService.Instance;
                var currentVersion = updateService.GetCurrentVersion();

                // 버전 정보 가져오기
                var versionInfo = await updateService.CheckForUpdateAsync();

                if (versionInfo == null)
                {
                    // 버전 정보를 가져올 수 없는 경우 계속 진행
                    return true;
                }

                // 새 버전이 있는지 확인
                if (updateService.IsNewVersionAvailable(currentVersion, versionInfo.Version))
                {
                    string message = $"새 버전({versionInfo.Version})이 있습니다.\n" +
                                   $"현재 버전: {currentVersion}\n\n";

                    // 강제 업데이트 여부에 따라 메시지 변경
                    if (versionInfo.IsForceUpdate)
                    {
                        message += "필수 업데이트입니다. 지금 업데이트하시겠습니까?";

                        var result = MessageBox.Show(
                            message,
                            "필수 업데이트",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            await DownloadAndInstallUpdateAsync(versionInfo.DownloadUrl);
                            return false; // 업데이트 설치 후 앱 종료
                        }
                        else
                        {
                            // 필수 업데이트 거부 시 앱 종료
                            MessageBox.Show(
                                "필수 업데이트를 설치하지 않으면 앱을 사용할 수 없습니다.",
                                "알림",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return false;
                        }
                    }
                    else
                    {
                        message += "지금 업데이트하시겠습니까?\n\n" +
                                 $"릴리스 노트:\n{versionInfo.ReleaseNotes}";

                        var result = MessageBox.Show(
                            message,
                            "업데이트 알림",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            await DownloadAndInstallUpdateAsync(versionInfo.DownloadUrl);
                            return false; // 업데이트 설치 후 앱 종료
                        }
                        // 선택적 업데이트는 거부해도 계속 진행
                    }
                }

                return true; // 업데이트 없거나 건너뛰기 선택
            }
            catch (Exception ex)
            {
                // 업데이트 체크 실패 시 로그만 남기고 계속 진행
                System.Diagnostics.Debug.WriteLine($"업데이트 체크 실패: {ex.Message}");

                // 네트워크 오류 등의 경우 앱 실행을 막지 않음
                var result = MessageBox.Show(
                    $"업데이트 확인 중 오류가 발생했습니다.\n{ex.Message}\n\n계속 진행하시겠습니까?",
                    "업데이트 확인 오류",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                return result == MessageBoxResult.Yes;
            }
        }

        private async Task DownloadAndInstallUpdateAsync(string downloadUrl)
        {
            UpdateProgressWindow progressWindow = null;

            try
            {
                // Progress 창 생성 및 표시
                progressWindow = new UpdateProgressWindow();
                progressWindow.Show();

                // Progress 콜백 생성
                var progress = new Progress<(int percent, long bytesReceived, long totalBytes)>(data =>
                {
                    progressWindow.UpdateProgress(data.percent, data.bytesReceived, data.totalBytes);
                });

                // 다운로드 및 설치
                await UpdateService.Instance.DownloadAndInstallUpdateAsync(downloadUrl, progress);

                // 완료 메시지를 잠시 보여주기
                await Task.Delay(1500);

                progressWindow.Close();
            }
            catch (Exception ex)
            {
                progressWindow?.Close();

                MessageBox.Show(
                    $"업데이트 설치 실패:\n{ex.Message}",
                    "오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}